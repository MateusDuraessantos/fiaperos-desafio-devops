"""Executa as suites de testes e verifica a cobertura das operacoes HTTP."""
import argparse
import json
import os
from pathlib import Path
import shutil
import subprocess
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]


def run(*args):
    print("+ " + " ".join(map(str, args)), flush=True)
    result = subprocess.run(list(map(str, args)), cwd=ROOT)
    if result.returncode:
        raise SystemExit(result.returncode)


def verify(results):
    summaries = {}
    for suite in ("unit", "bdd"):
        tree = ET.parse(results / f"{suite}.trx")
        counters = tree.find(".//{*}Counters")
        if counters is None:
            raise SystemExit(f"Contadores ausentes: {suite}")
        counts = {key: int(value) for key, value in counters.attrib.items()}
        if counts.get("total", 0) == 0 or counts.get("passed") != counts.get("total"):
            raise SystemExit(f"Suíte vazia, falhou ou teve testes ignorados: {suite}: {counts}")
        if suite == "bdd" and counts["passed"] < 3:
            raise SystemExit("A atividade exige pelo menos três cenários BDD.")
        summaries[suite] = counts

    traces = [json.loads(path.read_text(encoding="utf-8")) for path in (results / "http").glob("*.json")]
    if len(traces) != summaries["bdd"]["passed"] or not all(t["passed"] for t in traces):
        raise SystemExit("Os registros HTTP não correspondem à execução BDD atual.")
    openapi = json.loads((results / "openapi.json").read_text(encoding="utf-8"))
    expected = {(method.upper(), path) for path, methods in openapi["paths"].items()
                for method in methods if method in {"get", "post", "put", "delete", "patch"}}
    observed = {}
    for trace in traces:
        for exchange in trace["exchanges"]:
            if not exchange["statusValidated"] or not (exchange["contract"] or exchange["emptyBodyValidated"]):
                raise SystemExit(f"Resposta sem validação de status/contrato/corpo vazio: {exchange['method']} {exchange['path']}")
            path = exchange["path"].split("?")[0]
            # Map the recorded URL to an actual OpenAPI path template.
            for method, template in expected:
                if method != exchange["method"]:
                    continue
                actual_parts, template_parts = path.split("/"), template.split("/")
                if len(actual_parts) == len(template_parts) and all(
                    a == b or (b.startswith("{") and b.endswith("}") and a.isdigit())
                    for a, b in zip(actual_parts, template_parts)
                ):
                    observed.setdefault((method, template), set()).add(exchange["status"])
    missing = [f"{m} {p}" for m, p in sorted(expected)
               if not any(200 <= s < 300 for s in observed.get((m, p), set()))]
    if missing:
        raise SystemExit("Operações sem teste positivo HTTP: " + ", ".join(missing))
    summary = {
        "provider": sorted({trace["provider"] for trace in traces}),
        "startedUtc": min(t["startedUtc"] for t in traces),
        "finishedUtc": max(t["finishedUtc"] for t in traces),
        "suites": summaries,
        "operationsCovered": len(expected),
        "operations": [{"method": m, "path": p, "statuses": sorted(observed[(m, p)])}
                       for m, p in sorted(expected)]
    }
    (results / "summary.json").write_text(json.dumps(summary, ensure_ascii=False, indent=2), encoding="utf-8")
    lines = ["# Resultado dos testes", "", f"Banco: {', '.join(summary['provider'])}",
             f"Unitários: {summaries['unit']['passed']} aprovados.",
             f"BDD: {summaries['bdd']['passed']} aprovados.",
             f"Operações HTTP cobertas: {len(expected)}/{len(expected)}.", "",
             "| Método | Rota | Status observados |", "|---|---|---|"]
    lines += [f"| {m} | {p} | {', '.join(map(str, sorted(observed[(m, p)])))} |" for m, p in sorted(expected)]
    (results / "summary.md").write_text("\n".join(lines) + "\n", encoding="utf-8")
    print("\n".join(lines), flush=True)
    if os.environ.get("GITHUB_STEP_SUMMARY"):
        with open(os.environ["GITHUB_STEP_SUMMARY"], "a", encoding="utf-8") as stream:
            stream.write("\n".join(lines) + "\n")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--no-build", action="store_true", help="Usar build Release já realizado")
    parser.add_argument("--provider", choices=("sqlite", "sqlserver"), default="sqlite")
    args = parser.parse_args()
    if args.provider == "sqlserver" and not os.environ.get("BDD_SQLSERVER_CONNECTION"):
        parser.error("Configure BDD_SQLSERVER_CONNECTION para uma instância exclusiva de testes.")
    if args.provider == "sqlite":
        os.environ.pop("BDD_SQLSERVER_CONNECTION", None)
    results = ROOT / "test-results" / args.provider
    # Delete only a fixed, checked test output directory, so stale reports cannot pass the gate.
    if results.resolve().parent != (ROOT / "test-results").resolve():
        raise SystemExit("Diretório de resultados fora da pasta esperada.")
    if results.exists():
        shutil.rmtree(results)
    results.mkdir(parents=True)
    os.environ["BDD_RESULTS_DIRECTORY"] = str(results)
    os.environ["REQNROLL_FORMATTERS_HTML"] = "outputFilePath=" + str(results / "bdd-report.html")
    os.environ["REQNROLL_FORMATTERS_MESSAGE"] = "outputFilePath=" + str(results / "bdd-messages.ndjson")
    os.environ["DOTNET_CLI_WORKLOAD_UPDATE_NOTIFY_DISABLE"] = "true"
    os.environ["MSBuildEnableWorkloadResolver"] = "false"
    if not args.no_build:
        run("dotnet", "restore", "Greenhouse.sln", "--nologo")
        run("dotnet", "build", "Greenhouse.sln", "-c", "Release", "--no-restore", "--nologo")
    # Try both suites even when one fails, preserving both reports for diagnosis.
    failures = []
    for suite, project in (("unit", "Greenhouse.API.Tests"), ("bdd", "Greenhouse.API.BddTests")):
        try:
            run("dotnet", "test", f"tests/{project}/{project}.csproj", "-c", "Release", "--no-build", "--no-restore",
                "--nologo", "--logger", f"trx;LogFileName={suite}.trx", "--results-directory", results)
        except SystemExit as error:
            failures.append(error.code)
    if failures:
        raise SystemExit(failures[0])
    verify(results)


if __name__ == "__main__":
    main()
