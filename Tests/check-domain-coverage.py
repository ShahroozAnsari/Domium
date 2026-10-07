#!/usr/bin/env python3
"""Fail closed on missing reports or falling domain coverage; no test/source exclusions."""
import sys
import xml.etree.ElementTree as ET

if len(sys.argv) < 3:
    sys.exit("Usage: check-domain-coverage.py merged-Cobertura.xml Assembly [Assembly...]")
try:
    report = ET.parse(sys.argv[1]).getroot()
except (OSError, ET.ParseError) as error:
    sys.exit(f"Cannot read domain coverage: {error}")
packages = {item.attrib["name"]: item for item in report.findall("./packages/package")}
failed = False
for name in sys.argv[2:]:
    package = packages.get(name)
    if package is None or not package.findall("./classes/class"):
        print(f"FAIL {name}: missing coverage; zero tests is not success")
        failed = True
        continue
    lines = float(package.attrib["line-rate"]) * 100
    branches = float(package.attrib["branch-rate"]) * 100
    passed = lines >= 90 and branches >= 85
    print(f"{'PASS' if passed else 'FAIL'} {name}: lines {lines:.2f}% / branches {branches:.2f}% (minimum 90% / 85%)")
    failed |= not passed
sys.exit(1 if failed else 0)
