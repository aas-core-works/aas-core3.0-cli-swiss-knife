"""Generate a verifier transformer which keeps track of JSON paths."""
import io
import os
import pathlib
import sys
import textwrap
from typing import (
    List,
)

import aas_core_codegen.run
import aas_core_meta.v3
from aas_core_codegen import intermediate, naming
from aas_core_codegen.common import (
    Stripped,
    assert_never,
    Identifier,
    indent_but_first_line,
)
from aas_core_codegen.csharp import (
    common as csharp_common,
    naming as csharp_naming,
)
from aas_core_codegen.csharp.common import (
    INDENT as I,
    INDENT2 as II,
    INDENT3 as III,
    INDENT4 as IIII,
)
from icontract import ensure

import aas_core3_0_sk_codegen.common


def _generate_transform_property(
    prop: intermediate.Property,
) -> Stripped:
    """
    Generate the snippet to transform a property to errors by simply descending.

    Empty snippet means no descent into the property.
    """
    type_anno = intermediate.beneath_optional(prop.type_annotation)

    statements = []  # type: List[Stripped]

    prop_name = csharp_naming.property_name(prop.name)
    json_prop_literal = csharp_common.string_literal(naming.json_property(prop.name))

    if isinstance(type_anno, intermediate.PrimitiveTypeAnnotation):
        # NOTE (mristin, 2023-04-05):
        # No descent into a primitive.
        pass
    elif isinstance(type_anno, intermediate.OurTypeAnnotation):
        if isinstance(type_anno.our_type, intermediate.Enumeration):
            # NOTE (mristin, 2023-04-05):
            # No descent into an enumeration.
            pass
        elif isinstance(type_anno.our_type, intermediate.ConstrainedPrimitive):
            # NOTE (mristin, 2023-04-05):
            # No descent into a constrained primitive.
            pass
        elif isinstance(
            type_anno.our_type, (intermediate.AbstractClass, intermediate.ConcreteClass)
        ):
            statements.append(
                Stripped(
                    f"""\
foreach (
{I}var error in 
{I}Transform(that.{prop_name})
)
{{
{I}error.PrependSegment(
{II}new Aas.Reporting.NameSegment(
{III}{json_prop_literal}));
{I}yield return error;
}}"""
                )
            )
        else:
            assert_never(type_anno.our_type)
    elif isinstance(type_anno, intermediate.ListTypeAnnotation):
        assert isinstance(
            type_anno.items, intermediate.OurTypeAnnotation
        ) and isinstance(
            type_anno.items.our_type,
            (intermediate.AbstractClass, intermediate.ConcreteClass),
        ), (
            f"NOTE (mristin, 2023-03-29): We expect only lists of classes "
            f"at the moment, but you specified {type_anno}. "
            f"Please contact the developers if you need this feature."
        )

        index_var = csharp_naming.variable_name(Identifier(f"index_{prop.name}"))
        statements.append(
            Stripped(
                f"""\
int {index_var}  = 0;
foreach (
{I}var item in
{I}that.{prop_name}
)
{{
{I}foreach (
{II}var error in 
{II}Transform(that.{prop_name}[{index_var}])
{I})
{I}{{
{II}error.PrependSegment(
{III}new Aas.Reporting.IndexSegment(
{IIII}{index_var}
{III})
{II});
{II}error.PrependSegment(
{III}new Aas.Reporting.NameSegment(
{IIII}{json_prop_literal}
{III})
{II});
{II}yield return error;
{I}}}
{I}{index_var}++;
}}"""
            )
        )
    else:
        assert_never(type_anno)

    if len(statements) == 0:
        return Stripped("")

    statements_joined = Stripped("\n\n".join(statements))

    if isinstance(prop.type_annotation, intermediate.OptionalTypeAnnotation):
        return Stripped(
            f"""\
if (that.{prop_name} != null)
{{
{I}{indent_but_first_line(statements_joined, I)}
}}"""
        )

    return statements_joined


def _generate_transform_for_class(cls: intermediate.ConcreteClass) -> Stripped:
    """Generate the transform method to errors for the given concrete class."""
    blocks = []  # type: List[Stripped]

    name = csharp_naming.class_name(cls.name)

    for prop in cls.properties:
        block = _generate_transform_property(prop=prop)
        if len(block) == 0:
            continue

        blocks.append(block)

    if len(blocks) == 0:
        blocks.append(
            Stripped(
                f"""\
// Nothing defined for {name}.
yield break;"""
            )
        )

    writer = io.StringIO()

    interface_name = csharp_naming.interface_name(cls.name)
    transform_name = csharp_naming.method_name(Identifier(f"transform_{cls.name}"))

    writer.write(
        f"""\
public override IEnumerable<Aas.Reporting.Error> {transform_name}(
{I}Aas.{interface_name} that
)
{{
"""
    )

    for i, block in enumerate(blocks):
        if i > 0:
            writer.write("\n\n")
        writer.write(textwrap.indent(block, I))

    writer.write("\n}")

    return Stripped(writer.getvalue())


def _generate_transformer(symbol_table: intermediate.SymbolTable) -> Stripped:
    """Generate a transformer to double-dispatch an instance to errors."""
    blocks = []  # type: List[Stripped]

    for our_type in symbol_table.our_types:
        if not isinstance(our_type, intermediate.ConcreteClass):
            continue

        if our_type.is_implementation_specific:
            raise AssertionError(
                f"Expected no implementation-specific types, "
                f"but got: {our_type.name!r}"
            )

        block = _generate_transform_for_class(cls=our_type)
        blocks.append(block)

    writer = io.StringIO()
    writer.write(
        f"""\
public class PassThruVerifierWithJsonPaths
{I}: Aas.Visitation.AbstractTransformer<IEnumerable<Aas.Reporting.Error>>
{{
"""
    )

    for i, block in enumerate(blocks):
        if i > 0:
            writer.write("\n\n")
        writer.write(textwrap.indent(block, I))

    writer.write("\n}  // public class PassThruVerifierWithJsonPaths")

    return Stripped(writer.getvalue())


# fmt: off
@ensure(
    lambda result:
    result.endswith('\n'),
    "Trailing newline mandatory for valid end-of-files"
)
# fmt: on
def _generate(symbol_table: intermediate.SymbolTable) -> str:
    """Generate the C# code of the pass-thru verifier based on the meta-model."""
    blocks = [
        aas_core3_0_sk_codegen.common.WARNING,
        Stripped(
            """\
using Aas = AasCore.Aas3_0; // renamed 

using System.Collections.Generic;  // can't alias"""
        ),
    ]  # type: List[Stripped]

    transformer_block = _generate_transformer(symbol_table=symbol_table)

    blocks.append(
        Stripped(
            f"""\
namespace ListDanglingModelReferences
{{
{I}{indent_but_first_line(transformer_block, I)}
}}  // namespace ListDanglingModelReferences"""
        )
    )

    blocks.append(aas_core3_0_sk_codegen.common.WARNING)

    out = io.StringIO()
    for i, block in enumerate(blocks):
        if i > 0:
            out.write("\n\n")

        out.write(block)

    out.write("\n")

    return out.getvalue()


def main() -> int:
    """Execute the main routine."""
    model_path = pathlib.Path(aas_core_meta.v3.__file__)
    assert model_path.exists() and model_path.is_file(), model_path

    symbol_table_atok, error_message = aas_core_codegen.run.load_model(model_path)
    assert error_message is None, f"{error_message=}"

    symbol_table, _ = symbol_table_atok

    code = _generate(symbol_table=symbol_table)

    this_path = pathlib.Path(os.path.realpath(__file__))
    repo_root = this_path.parent.parent.parent

    target_pth = (
        repo_root
        / "src/ListDanglingModelReferences/PassThruVerifierWithJsonPaths.generated.cs"
    )
    with target_pth.open("wt") as fid:
        fid.write(code)
        fid.write("\n")

    return 0


if __name__ == "__main__":
    sys.exit(main())
