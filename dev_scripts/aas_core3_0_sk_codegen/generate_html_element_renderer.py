"""Generate a transformer that renders element to static HTML."""
import html
import io
import os
import pathlib
import re
import sys
import textwrap
from typing import (
    List,
)

import aas_core_codegen.run
import aas_core_meta.v3
from aas_core_codegen import intermediate
from aas_core_codegen.common import (
    Stripped,
    assert_never,
    Identifier,
    indent_but_first_line,
)
from aas_core_codegen.csharp import (
    naming as csharp_naming,
)
from aas_core_codegen.csharp.common import (
    INDENT as I,
    INDENT2 as II,
    INDENT3 as III,
    INDENT4 as IIII,
    INDENT5 as IIIII,
)
from icontract import ensure

import aas_core3_0_sk_codegen.common


def human_readable_property_name(property_name: Identifier) -> Stripped:
    """Return the property name such that it can be human-readable."""
    # NOTE (mristin, 2023-09-06):
    # ID-short is an exception that requires a hyphen for readability.
    if property_name == "ID_short":
        return Stripped("ID-short")

    readable = property_name.replace("_", " ")
    if not readable[0].isupper():
        return Stripped(readable[0].upper() + readable[1:])

    return Stripped(readable)


CSS_CLASS_RE = re.compile(r"^[a-zA-Z_\d-]+$")


@ensure(
    lambda result: CSS_CLASS_RE.match(result) is not None,
    "CSS class name can be used in HTML without any escaping.",
)
def css_class_name(class_name: Identifier) -> Stripped:
    """Return the CSS class name corresponding to the AAS class."""
    kebab_case = class_name.lower().replace("_", "-")
    return Stripped(f"aas-{kebab_case}")


# fmt: off
@ensure(
    lambda result: len(result) > 0,
    "The generated code must be non-empty for a property."
)
# fmt: on
def _generate_transform_property(
    prop: intermediate.Property,
) -> Stripped:
    """Generate the snippet to transform a property to an HTML snippet."""
    type_anno = intermediate.beneath_optional(prop.type_annotation)

    prop_name = csharp_naming.property_name(prop.name)
    human_readable_prop_name = human_readable_property_name(prop.name)

    statements = [
        Stripped(
            f"""\
parts.Add(
{I}"<div class='label'>\\n" +
{I}"{html.escape(human_readable_prop_name)}\\n" +
{I}"</div>"
);"""
        )
    ]  # type: List[Stripped]

    primitive_type = intermediate.try_primitive_type(type_anno)

    if isinstance(type_anno, intermediate.PrimitiveTypeAnnotation) or (
        isinstance(type_anno, intermediate.OurTypeAnnotation)
        and isinstance(type_anno.our_type, intermediate.ConstrainedPrimitive)
    ):
        assert primitive_type is not None
        if primitive_type is intermediate.PrimitiveType.BOOL:
            # NOTE (mristin, 2023-09-06):
            # Cast prefix is needed since the compiler can not infer the boolean
            # automatically if it is a nullable.
            cast_prefix = ""
            if isinstance(prop.type_annotation, intermediate.OptionalTypeAnnotation):
                cast_prefix = "(bool)"

            statements.append(
                Stripped(
                    f"""\
parts.Add(
{I}"<div class='bool'>\\n" +
{I}$"{{({cast_prefix}that.{prop_name} ? "true" : "false")}}\\n" +
{I}"</div>"
);"""
                )
            )
        elif primitive_type is intermediate.PrimitiveType.INT:
            statements.append(
                Stripped(
                    f"""\
parts.Add(
{I}"<div class='int'>\\n" +
{I}$"{{System.Web.HttpUtility.HtmlEncode(that.{prop_name}.ToString())}}\\n"
{I}"</div>"
);"""
                )
            )
        elif primitive_type is intermediate.PrimitiveType.INT:
            statements.append(
                Stripped(
                    f"""\
parts.Add(
{I}"<div class='float'>\\n" +
{I}$"{{System.Web.HttpUtility.HtmlEncode(that.{prop_name}.ToString())}}\\n"
{I}"</div>"
);"""
                )
            )
        elif primitive_type is intermediate.PrimitiveType.STR:
            statements.append(
                Stripped(
                    f"""\
parts.Add(
{I}"<div class='str'>\\n" +
{I}$"{{System.Web.HttpUtility.HtmlEncode(that.{prop_name})}}\\n" +
{I}"</div>"
);"""
                )
            )
        elif primitive_type is intermediate.PrimitiveType.BYTEARRAY:
            statements.append(
                Stripped(
                    f"""\
parts.Add(
{I}"<div class='bytearray'>\\n" +
{I}$"{{that.{prop_name}.Length}} byte(s)\\n" +
{I}"</div>"
);"""
                )
            )
        else:
            assert_never(primitive_type)

    elif isinstance(type_anno, intermediate.OurTypeAnnotation):
        if isinstance(type_anno.our_type, intermediate.Enumeration):
            statements.append(
                Stripped(
                    f"""\
parts.Add(
{I}"<div class='enumeration'>\\n" +
{I}System.Web.HttpUtility.HtmlEncode(
{II}Aas.Stringification.ToString(
{III}that.{prop_name}
{II}) ?? "Invalid value"
{I}) + "\\n" +
{I}"</div>"
);"""
                )
            )

        elif isinstance(type_anno.our_type, intermediate.ConstrainedPrimitive):
            raise AssertionError("Constrained primitives must have been handled before")

        elif isinstance(
            type_anno.our_type, (intermediate.AbstractClass, intermediate.ConcreteClass)
        ):
            statements.append(
                Stripped(
                    f"""\
parts.Add(
{I}this.Transform(
{II}that.{prop_name}
{I})
);"""
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
            f"NOTE (mristin, 2023-09-06): We expect only lists of classes "
            f"at the moment, but you specified {type_anno}. "
            f"Please contact the developers if you need this feature."
        )

        part_var = csharp_naming.variable_name(Identifier(f"part_{prop.name}"))

        statements.append(
            Stripped(
                f"""\
var {part_var} = new List<string>()
{I}{{
{II}"<div class='list'>\\n"
{I}}};

foreach (
{I}var item in
{I}that.{prop_name}
)
{{
{I}{part_var}.Add(this.Transform(item));
}}

{part_var}.Add("</div>");
parts.Add(string.Join("\\n", {part_var}));"""
            )
        )
    else:
        assert_never(type_anno)

    assert len(statements) > 0, "At least one statement expected to render the property"
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
    """Generate the transform method to HTML snippet for the given concrete class."""
    blocks = []  # type: List[Stripped]

    name = csharp_naming.class_name(cls.name)
    css_name = css_class_name(cls.name)
    assert "'" not in css_name

    for prop in cls.properties:
        block = _generate_transform_property(prop=prop)
        blocks.append(
            Stripped(
                f"""\
// Render {csharp_naming.property_name(prop.name)}
parts.Add(
{I}"<div class='property'>" +
{I}"<!-- {html.escape(human_readable_property_name(prop.name))} -->"
);
{block}
parts.Add("</div>");"""
            )
        )

    if len(blocks) == 0:
        blocks.append(
            Stripped(
                f"""\
// Nothing to be rendered for the class {name}.
return "<div class='embedded {css_name}'></div>";"""
            )
        )
    else:
        blocks.insert(0, Stripped(f"""var parts = new List<string>();"""))

        blocks.append(
            Stripped(
                f"""\
return
{I}"<div class='embedded {css_name}'>\\n" +
{I}$"{{string.Join("\\n", parts)}}\\n" +
{I}"</div>";"""
            )
        )

    writer = io.StringIO()

    interface_name = csharp_naming.interface_name(cls.name)
    transform_name = csharp_naming.method_name(Identifier(f"transform_{cls.name}"))

    writer.write(
        f"""\
public override string {transform_name}(
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
    """Generate a transformer to double-dispatch to render an instance."""
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
/**
 * Render a model instance as a HTML snippet.
 *
 * All the transform methods are automatically generated. You probably want
 * to inherit from this class and override one or the other transformation.
 */
internal class GeneratedElementRenderer
{I}: Aas.Visitation.AbstractTransformer<string>
{{
"""
    )

    for i, block in enumerate(blocks):
        if i > 0:
            writer.write("\n\n")
        writer.write(textwrap.indent(block, I))

    writer.write("\n}  // internal class ElementRenderer")

    return Stripped(writer.getvalue())


# fmt: off
@ensure(
    lambda result:
    result.endswith('\n'),
    "Trailing newline mandatory for valid end-of-files"
)
# fmt: on
def _generate(symbol_table: intermediate.SymbolTable) -> str:
    """Generate the C# code of the element renderer to HTML based on the meta-model."""
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
namespace RenderEnvironmentToHtml
{{
{I}{indent_but_first_line(transformer_block, I)}
}}  // namespace RenderEnvironmentToHtml"""
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
        repo_root / "src/RenderEnvironmentToHtml/GeneratedElementRenderer.generated.cs"
    )
    with target_pth.open("wt") as fid:
        fid.write(code)
        fid.write("\n")

    return 0


if __name__ == "__main__":
    sys.exit(main())
