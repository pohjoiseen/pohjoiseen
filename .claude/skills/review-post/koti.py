#!/usr/bin/env python3
"""Command-line access to KoTi API for reviewing content (see SKILL.md next to this file).

    koti.py list [post|article|book] [language]      all items, without text
    koti.py get <post|article|book> <id>             Markdown text (ContentMD) to stdout
    koti.py info <post|article|book> <id>            everything except the text, as JSON
    koti.py suggestions <post|article|book> <id>     existing suggestions, as JSON
    koti.py check <file.json>                        validate a suggestions file against current text, post nothing
    koti.py suggest <file.json>                      validate and create suggestions
    koti.py withdraw <batch>                         delete pending suggestions of a batch
    koti.py revisions <post|article|book> <id>       history of the text, without texts
    koti.py revision <id>                            text of one revision to stdout

Credentials are never handled here: curl reads them from the netrc file (KOTI_NETRC, default
~/.config/koti/netrc).  Server is KOTI_URL, default https://koti.pohjoiseen.fi (for a local KoTi set
KOTI_URL=https://localhost:7169 KOTI_INSECURE=1).
"""
import json
import os
import subprocess
import sys

BASE = os.environ.get("KOTI_URL", "https://koti.pohjoiseen.fi").rstrip("/")
NETRC = os.path.expanduser(os.environ.get("KOTI_NETRC", "~/.config/koti/netrc"))
PLURAL = {"post": "Posts", "article": "Articles", "book": "Books"}
KINDS = {"correction", "update", "link", "other"}


def call(method, path, body=None):
    cmd = ["curl", "-sS", "-m", "60", "-X", method, "-w", "\n%{http_code}", BASE + path]
    if os.path.exists(NETRC):
        cmd += ["--netrc-file", NETRC]
    if os.environ.get("KOTI_INSECURE"):
        cmd.append("-k")
    if body is not None:
        cmd += ["-H", "Content-Type: application/json", "--data-binary", json.dumps(body, ensure_ascii=False)]
    result = subprocess.run(cmd, capture_output=True, text=True)
    if result.returncode != 0:
        sys.exit("curl failed: " + result.stderr.strip())
    out, _, status = result.stdout.rpartition("\n")
    if not status.startswith("2"):
        sys.exit("HTTP %s from %s %s: %s" % (status, method, path, out[:500]))
    return json.loads(out) if out.strip() else None


def content_of(kind, target_id):
    return call("GET", "/api/%s/%s" % (PLURAL[kind], target_id))["contentMD"].replace("\r\n", "\n")


def check(data):
    """Same checks as the server makes, plus overlaps; returns a list of error strings."""
    errors = []
    kind, target_id = data.get("targetType"), data.get("targetId")
    if kind not in PLURAL or not isinstance(target_id, int):
        return ['"targetType" must be post, article or book, and "targetId" a number']
    if not str(data.get("batch", "")).strip():
        errors.append('"batch" is required')
    content = content_of(kind, target_id)
    spans = []
    for i, s in enumerate(data.get("suggestions", [])):
        where = "suggestion %d" % i
        old, new = s.get("oldText"), s.get("newText")
        if s.get("kind") not in KINDS:
            errors.append(where + ": kind must be one of " + ", ".join(sorted(KINDS)))
        if not isinstance(old, str) or not old.strip() or not isinstance(new, str):
            errors.append(where + ": oldText and newText must be strings, oldText not empty")
            continue
        old = old.replace("\r\n", "\n")
        if old == new:
            errors.append(where + ": oldText and newText are the same")
        if not str(s.get("comment", "")).strip():
            errors.append(where + ": comment is required")
        n = content.count(old)
        if n == 0:
            errors.append(where + ": oldText not found in the text (must match exactly): %r" % old[:80])
        elif n > 1:
            errors.append(where + ": oldText occurs %d times, add surrounding text: %r" % (n, old[:80]))
        else:
            start = content.index(old)
            spans.append((start, start + len(old), i))
    spans.sort()
    for (s1, e1, i1), (s2, e2, i2) in zip(spans, spans[1:]):
        if s2 < e1:
            errors.append("suggestions %d and %d overlap in the text, merge them or shorten oldText" % (i1, i2))
    return errors


def main(argv):
    if len(argv) < 2:
        sys.exit(__doc__)
    command, args = argv[1], argv[2:]
    if command == "list":
        kind = args[0] if args else "post"
        language = "?language=" + args[1] if len(args) > 1 else ""
        for item in call("GET", "/api/%s%s" % (PLURAL[kind], language)):
            print("%5d  %s  %-10s %-45s %6d chars  %s%s%s" % (
                item["id"], item["language"], item.get("date", "")[:10], item["name"], item["contentLength"],
                item["title"], "  [draft]" if item["draft"] else "",
                "  [%d pending]" % item["pendingSuggestions"] if item["pendingSuggestions"] else ""))
    elif command == "get":
        sys.stdout.write(content_of(args[0], int(args[1])))
    elif command == "info":
        item = call("GET", "/api/%s/%s" % (PLURAL[args[0]], args[1]))
        item.pop("contentMD", None)
        print(json.dumps(item, ensure_ascii=False, indent=1))
    elif command == "suggestions":
        print(json.dumps(call("GET", "/api/Suggestions?targetType=%s&targetId=%s" % (args[0], args[1])),
                         ensure_ascii=False, indent=1))
    elif command in ("check", "suggest"):
        data = json.load(open(args[0], encoding="utf-8"))
        errors = check(data)
        if errors:
            print("%d error(s), nothing posted:" % len(errors))
            for e in errors:
                print("  - " + e)
            sys.exit(1)
        if command == "check":
            print("OK (%d suggestions)" % len(data.get("suggestions", [])))
            return
        body = {"targetType": data["targetType"], "targetId": data["targetId"], "batch": data["batch"],
                "suggestions": [{k: s.get(k) for k in ("kind", "oldText", "newText", "comment", "sources")}
                                for s in data["suggestions"]]}
        result = call("POST", "/api/Suggestions", body)
        print("created %d, rejected %d" % (len(result["created"]), len(result["rejected"])))
        for r in result["rejected"]:
            print("  - suggestion %d: %s" % (r["index"], r["error"]))
    elif command == "withdraw":
        print("deleted %s pending suggestions" % call("DELETE", "/api/Suggestions?batch=" + args[0]))
    elif command == "revisions":
        for r in call("GET", "/api/Revisions?targetType=%s&targetId=%s" % (args[0], args[1])):
            print("%6d  %s  v%-4d %7d chars%s" % (r["id"], r["savedAt"][:16], r["version"], r["contentLength"],
                                                  "  kept" if r["keep"] else ""))
    elif command == "revision":
        sys.stdout.write(call("GET", "/api/Revisions/" + args[0])["contentMD"])
    else:
        sys.exit(__doc__)


if __name__ == "__main__":
    main(sys.argv)
