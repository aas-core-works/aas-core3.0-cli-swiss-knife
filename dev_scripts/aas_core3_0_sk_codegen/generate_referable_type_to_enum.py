import io
import os
import pathlib
import sys
import textwrap
from typing import List, Optional, Tuple

import aas_core_meta.v3
import aas_core_codegen
import aas_core_codegen.common
import aas_core_codegen.csharp.naming
import aas_core_codegen.naming
import aas_core_codegen.parse
import aas_core_codegen.run
from aas_core_codegen import intermediate
from aas_core_codegen.common import Stripped, Identifier, indent_but_first_line
from aas_core_codegen.csharp import (
    common as csharp_common,
    naming as csharp_naming,
)
from aas_core_codegen.csharp.common import (
    INDENT as I,
    INDENT2 as II,
    INDENT3 as III,
)
import aas_core_codegen.run

import aas_core3_0_sk_codegen.common


def _generate_mapping_function(symbol_table: intermediate.SymbolTable) -> Stripped:
    """Generate the module to map C# class types to KeyTypes enum literals."""
    referable_cls = symbol_table.must_find_abstract_class(Identifier("Referable"))

    key_types_enum = symbol_table.must_find_enumeration(Identifier("Key_types"))

    # NOTE (mristin, 2023-04-05):
    # We can not use type-switch here as there are concrete referable classes which
    # inherit from other concrete referable classes, so the order of the checks
    # does matter.

    classes_literals = (
        []
    )  # type: List[Tuple[intermediate.ConcreteClass, intermediate.EnumerationLiteral]]

    for our_type in symbol_table.our_types_topologically_sorted:
        if not isinstance(our_type, intermediate.ConcreteClass):
            continue

        if not our_type.is_subclass_of(referable_cls):
            continue

        literal = key_types_enum.literals_by_name.get(our_type.name, None)
        if literal is None:
            raise AssertionError(
                f"Could not find the literal in {key_types_enum.name!r} "
                f"corresponding to the class {our_type.name!r}"
            )

        classes_literals.append((our_type, literal))

    # NOTE (mristin, 2023-04-05):
    # We reverse the order, so that the most concrete classes come first. See the
    # remark regarding why we can not use type-switches above.
    classes_literals.reverse()

    blocks = [
        Stripped(
            f"""\
// NOTE (mristin, 2023-04-05):
// We can not use type-switch here as there are concrete referable classes which inherit
// from other concrete referable classes, so the order of the checks does matter."""
        )
    ]  # type: List[Stripped]

    for cls, literal in classes_literals:
        cls_name = csharp_naming.interface_name(cls.name)
        literal_name = csharp_naming.enum_literal_name(literal.name)

        blocks.append(
            Stripped(
                f"""\
if (that is Aas.{cls_name})
{{
{I}return Aas.KeyTypes.{literal_name};
}}"""
            )
        )

    blocks.append(
        Stripped(
            f"""\
throw new System.InvalidOperationException(
{I}$"Unexpected type: {{that.GetType()}}"
);"""
        )
    )

    blocks_joined = "\n\n".join(blocks)

    function_name = csharp_naming.method_name(Identifier("map"))

    return Stripped(
        f"""\
public static Aas.KeyTypes {function_name}(
{I}Aas.IReferable that
)
{{
{I}{indent_but_first_line(blocks_joined, I)}
}}"""
    )


def _generate(symbol_table: intermediate.SymbolTable) -> Stripped:
    """Generate the C# file."""

    code = _generate_mapping_function(symbol_table)

    blocks = [
        aas_core3_0_sk_codegen.common.WARNING,
        Stripped(
            f"""\
using Aas = AasCore.Aas3_0; // renamed"""
        ),
        Stripped(
            f"""\
namespace ListDanglingModelReferences {{
{I}public static class ReferableToKeyTypes
{I}{{
{II}{indent_but_first_line(code, II)}
{I}}} 
}}"""
        ),
        aas_core3_0_sk_codegen.common.WARNING,
    ]  # type: List[Stripped]

    return Stripped("\n\n".join(blocks))


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
        repo_root / "src/ListDanglingModelReferences/ReferableToKeyTypes.generated.cs"
    )
    with target_pth.open("wt") as fid:
        fid.write(code)
        fid.write("\n")

    return 0


if __name__ == "__main__":
    sys.exit(main())
