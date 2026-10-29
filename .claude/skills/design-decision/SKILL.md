---
name: design-decision
description: Handle any game design question for No Way Out — read the GDD, give a short diagnosis, offer 2–4 options with a ★ recommendation through AskUserQuestion, ask when to do it, then update GDD tags, the open-questions list and Docs/Decisions.md. Code only after the user picks. Use for new mechanics, balance, entities, illusions, UI/HUD behaviour, run structure, or when the user reports a design problem.
---

# Design decision

The user decides design. Claude prepares the decision and keeps the documents in sync.

## 1. Read first

- `Docs/GDD.md` (Russian): the pillars (§1), the related sections, and open questions (§16).
- `Docs/Decisions.md` for anything already decided.
- `Docs/OpenQuestions.md` for open questions and known contradictions. If the topic touches one, settle it in the same round.
- If code is affected, the matching section of `Docs/TechnicalOverview.md`.

## 2. Diagnose

Write 2–4 sentences in Russian:
- what the problem or goal is,
- which pillar it touches (asymmetry → conversation, space as the enemy, honest deception, situations over content),
- the constraints (fairness rule, collider rule, HUD truth rule, MVP scope, networking cost).

## 3. Options

Ask with **AskUserQuestion**:
- 2–4 options, the recommendation first, with "(Recommended)" in its label.
- Each option says what it is, **one concrete moment of play** ("you see the bridge, your friend says…"), the cost (code, art, network), and the main risk.
- Use `preview` for layouts, flows or numbers when it helps compare.
- **Many issues at once:** one numbered list in the message (context, options, ★), then ask about them in batches of up to 4 questions. The user answers by number.
- If the answer needs playtesting, say so and propose the smallest test.
- Then ask **when**: now, in the MVP, or later.

## 4. After the user picks

- **GDD:**
  - set the tag (**[DECIDED]**, or **[PROPOSED]** for numbers until they're playtested),
  - rewrite the section in Russian, keeping its style (short paragraphs, tables),
  - move the answered item in `Docs/OpenQuestions.md` to "Закрыто" (date and short result), and add any new contradiction found on the way,
  - update "Последнее обновление".
- **`Docs/Decisions.md`:** add a row with the date, area, decision and why. Move it out of "Open" if it was there.
- **Technical impact:** update `TechnicalOverview.md` (authority, systems, configs).
- **Implementation:** if the user said "now", do it in the same task. Otherwise list it as a follow-up in the report.

## Rules

- Never change a [DECIDED] item without asking. If new information conflicts with it, raise it as a new question.
- Don't invent lore, names or numbers silently. Propose them as [PROPOSED] and say so.
- Keep the GDD in Russian and everything else in English.
