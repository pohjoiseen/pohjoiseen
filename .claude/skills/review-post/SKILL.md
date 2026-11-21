---
name: review-post
description: Proofread and fact-check a blog post (or article/book) in KoTi and leave the findings there as suggested edits for the author to accept or decline. Use when asked to review, proofread, fact-check or check a post, e.g. "review post 607" or "check the new post about Kemi".
---

# Reviewing a post

The blog (fennica.pohjoiseen.fi) is written in Russian by one author, mostly about travels in Finland and nearby
countries. A review never changes any text: it creates *suggestions* in KoTi (the backoffice), which the author
goes through on the edit page, accepting, editing or declining each. So every suggestion must be small,
self-contained and easy to judge, and nothing is lost by suggesting a fix the author then declines.

Everything goes through `koti.py` in this directory (run `python3 .claude/skills/review-post/koti.py` without
arguments for usage). It talks to the deployed KoTi at https://koti.pohjoiseen.fi; credentials are in a netrc
file which curl reads, never print or read that file. In the sandbox the command needs `koti.pohjoiseen.fi`
in its allowed domains. The API is described in `KoTi/README.md` ("Suggestions").

## Procedure

1. **Find the post.** If given a name or a topic and not an id: `koti.py list post ru` and pick it; ask if it is
   ambiguous. Articles and books work the same (`article`, `book`).
2. **Read it.** `koti.py get post <id>` for the text, saved to a file in the scratchpad, and `koti.py info post <id>`
   for title, description, dates, location, address and public transport lines (these are shown in the post header
   and footer). Read the whole text. Check `koti.py suggestions post <id>` for pending suggestions from an earlier
   review, and do not repeat them.
3. **Proofread** (see below).
4. **Fact-check** (see below), with web search.
5. **Write the suggestions** into one JSON file in the scratchpad (format below), run `koti.py check <file>`, fix what
   it reports, then `koti.py suggest <file>`.
6. **Report** to the author in chat: how many suggestions of which kind, the ones that most need their judgement,
   what you could not verify, and anything about the header/footer fields (those cannot be suggested, list
   them as plain text for fixing by hand). Give the edit link: `https://koti.pohjoiseen.fi/Posts/<id>/ru/`.

A batch gone wrong can be removed with `koti.py withdraw <batch>` (only suggestions still pending).

## Proofreading

Look for typos, wrong case/number/gender agreement, missing or doubled words, broken Markdown, a Latin letter typed
instead of a Cyrillic one (c/с, a/а, o/о, p/р, e/е, x/х), wrong transliteration of a name compared to its original
given in the same post, inconsistent spelling of the same name within the post, numbering gaps in numbered photo
paragraphs (report these in chat, do not renumber).

Do **not** touch style: the author writes informally, in the first person, with long sentences, colloquialisms
and deliberate jokes (including strikethrough `<s>...</s>`). Word choice, punctuation habits (`--` or `—`, two
spaces after a full stop, `"` quotes), letter ё or its absence, and sentence structure are theirs. If something
reads oddly but may be intended, leave it, or at most mention it in the report.

## Fact-checking

Go through the text and collect checkable claims: dates, numbers (population, distances, heights, lengths, prices),
names of people, places and companies and their spelling in the original language, "the first/largest/only"
claims, who built/founded/owned what, historical sequences, and present-tense statements about the world
(what operates, runs, belongs to whom, is open or closed, timetables, plans "to be finished in ...").

Verify with web search, preferring primary and local sources: municipality and company sites, Statistics Finland,
Fintraffic/operator timetables, Finnish/Swedish/Norwegian Wikipedia (usually better than English or Russian for
these places), Yle and local newspapers. Read the source, do not rely on a search snippet. Note a confidence for
each finding:

- **high**: a primary or two independent sources say so clearly;
- **medium**: one decent source, or sources slightly disagree;
- **low**: a guess, a snippet, memory. Low-confidence findings never become suggestions; mention the important
  ones in the report as things you could not settle.

Do not "correct" the author's own observations, impressions and opinions, or anything they saw themselves and
you only doubt. Do not add facts the post does not need.

## What to suggest and how to word it

The new text is published under the author's name, so it is in Russian, in their voice, and as short as possible.

1. **Corrections** (typos, factual slips, wrong names, dates, numbers): fix in place, changing as little as
   possible. The corrected text must read as if it had always been there. Never refer to what the passage said
   before ("не X, а Y", "на самом деле" as a contrast to the old claim): readers never saw the old version.
   Kind `correction`.
2. **Things that changed after the post was written** (only for posts already published some time ago; a new
   draft has none): keep the original sentence and add right after it a short italic note in parentheses starting
   with the current year, `_(2026: ...)_`, one or two plain sentences without source references. If the original
   says "сейчас" and that is now false, its tense may be minimally adjusted in the same suggestion. Figures which
   simply got newer (population, shares, prices) are replaced silently with the year added: `20.1 тыс. (2024)`.
   Dead or moved links are replaced when the new address is known. Kind `update`.
   Examples of notes written by the author:
   - `... сейчас действующий под брэндом Scandic _(2026: закрылся в ковид и был продан, открылся заново в 2023 как независимый)_.`
   - `_(2026: школы переехали в новое общее здание, построенное в 2022.)_`
   - `_(2026: с 2021 поменяли на "Давай потише", Ota rauhallisesti / Ta det lugnt)_`
   Inside picture captions (`![caption](picture:123)`) italics do not render, use plain `(2026: ...)` there.
3. **Links to other posts**: when the post mentions a place which has its own post and is not linked yet, suggest
   `[название](post:ID)` at the first mention only (ids from `koti.py list post ru`). Kind `link`.
4. Do not rewrite for style, do not merge unrelated fixes, do not change `picture:`/`post:`/`article:` references
   (other than adding a link as in 3) or HTML comments like `<!--gallery-->`.

## Suggestions file

```json
{
  "targetType": "post",
  "targetId": 607,
  "batch": "review-2026-11-21",
  "suggestions": [
    {
      "kind": "correction",
      "oldText": "exact fragment of the text",
      "newText": "what replaces it",
      "comment": "почему, по-русски, для автора",
      "sources": ["https://..."]
    }
  ]
}
```

- `batch`: `review-` and today's date, the same for everything from one review.
- `oldText` must be copied exactly from the text (same characters, spaces, line breaks, Markdown) and occur in it
  exactly once; line breaks inside paragraphs are real. Keep it short: the phrase being changed plus just enough
  words to be unique. `newText` repeats the unchanged part.
- To insert something, use the end of the preceding sentence as `oldText` and repeat it at the start of `newText`.
- Suggestions for one post must not overlap; if two findings concern the same sentence, make one suggestion.
- `comment`: in Russian, one to three sentences: what is wrong or what changed, the key facts, and the confidence
  if it is not high ("уверенность средняя"). Say so if the wording needs the author's eye, or if accepting means
  a nearby passage should be reread. For a plain typo just "Опечатка."
- `sources`: up to four URLs you actually read, the most direct ones. None for typos.

Only the text (`ContentMD`) can be changed by suggestions. Problems in the title, description, dates, address
or public transport lines go into the report.
