"""Package smoke tests invoked from ASP.NET Core through pythonnet.

Every test returns a plain dict so the result can cross the pythonnet
boundary without custom marshalling. Exceptions are caught per test and
reported as ``ok=False``; nothing is raised out of ``run_all``.
"""

from __future__ import annotations

import sys
import traceback
from importlib.metadata import version as _pkg_version
from typing import Callable


def _version(dist: str) -> str:
    try:
        return _pkg_version(dist)
    except Exception:  # pragma: no cover - only if metadata is missing
        return "unknown"


def test_duckdb() -> str:
    import duckdb

    row = duckdb.sql("select 40 + 2 as answer").fetchone()
    assert row == (42,), row
    return f"select 40 + 2 -> {row[0]}"


def test_jellyfish() -> str:
    import jellyfish

    score = jellyfish.jaro_winkler_similarity("martha", "marhta")
    assert 0.9 < score <= 1.0, score
    return f"jaro_winkler('martha','marhta') = {score:.4f}"


def test_polars() -> str:
    import polars as pl

    df = pl.DataFrame({"name": ["ann", "bob", "cid"], "age": [31, 45, 27]})
    adults_over_30 = df.filter(pl.col("age") > 30).height
    assert adults_over_30 == 2, adults_over_30
    return f"{df.height} rows, {adults_over_30} with age > 30"


def test_pyarrow() -> str:
    import pyarrow as pa
    import pyarrow.compute as pc

    table = pa.table({"x": [1, 2, 3, 4]})
    total = pc.sum(table["x"]).as_py()
    assert table.num_rows == 4 and total == 10, (table.num_rows, total)
    return f"{table.num_rows} rows, sum(x) = {total}"


def test_pydantic() -> str:
    from pydantic import BaseModel, ValidationError

    class Person(BaseModel):
        name: str
        age: int

    person = Person.model_validate({"name": "ann", "age": "31"})
    assert person.age == 31
    try:
        Person.model_validate({"name": "bob", "age": "not-a-number"})
    except ValidationError:
        return "validated good input, rejected bad input"
    raise AssertionError("bad input was not rejected")


def test_splink() -> str:
    import pandas as pd
    import splink.comparison_library as cl
    from splink import DuckDBAPI, Linker, SettingsCreator, block_on

    records = [
        {"unique_id": 1, "first_name": "robert", "surname": "smith", "city": "london"},
        {"unique_id": 2, "first_name": "robert", "surname": "smyth", "city": "london"},
        {"unique_id": 3, "first_name": "alice", "surname": "jones", "city": "leeds"},
        {"unique_id": 4, "first_name": "alice", "surname": "jones", "city": "leeds"},
        {"unique_id": 5, "first_name": "carol", "surname": "white", "city": "york"},
    ]
    settings = SettingsCreator(
        link_type="dedupe_only",
        blocking_rules_to_generate_predictions=[block_on("first_name")],
        comparisons=[
            cl.JaroWinklerAtThresholds("surname", [0.9, 0.7]),
            cl.ExactMatch("city"),
        ],
    )
    db_api = DuckDBAPI()
    # Splink treats a bare list as "a list of tables", so wrap records in a frame.
    linker = Linker(pd.DataFrame(records), settings, db_api=db_api)
    predictions = linker.inference.predict(threshold_match_probability=0.0)
    count = len(predictions.as_record_dict())
    assert count >= 2, count
    return f"dedupe of {len(records)} records produced {count} candidate pairs"


TESTS: dict[str, tuple[str, Callable[[], str]]] = {
    "duckdb": ("duckdb", test_duckdb),
    "jellyfish": ("jellyfish", test_jellyfish),
    "polars": ("polars", test_polars),
    "pyarrow": ("pyarrow", test_pyarrow),
    "pydantic": ("pydantic", test_pydantic),
    "splink": ("splink", test_splink),
}


def list_tests() -> list[str]:
    return list(TESTS)


def _run(name: str) -> dict:
    dist, fn = TESTS[name]
    result = {"name": name, "package": dist, "version": _version(dist), "ok": False, "detail": ""}
    try:
        result["detail"] = fn()
        result["ok"] = True
    except Exception as exc:  # report, never raise across the .NET boundary
        result["detail"] = f"{type(exc).__name__}: {exc}\n{traceback.format_exc()}"
    return result


def run_one(name: str) -> dict:
    if name not in TESTS:
        raise KeyError(name)
    return _run(name)


def run_all() -> list[dict]:
    return [_run(name) for name in TESTS]


def python_info() -> dict:
    return {"version": sys.version, "executable": sys.executable, "prefix": sys.prefix}


if __name__ == "__main__":
    results = run_all()
    for r in results:
        print(("PASS" if r["ok"] else "FAIL"), r["name"], r["version"], "-", r["detail"].splitlines()[0])
    sys.exit(0 if all(r["ok"] for r in results) else 1)
