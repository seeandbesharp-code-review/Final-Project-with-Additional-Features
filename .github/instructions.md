# AI Work Instructions for This Repository

## Purpose
This file describes how the AI should work with this project repository.

## General rules
- Treat this repository as a combined .NET API and Angular project.
- When the user asks about code, use the repository structure to keep answers focused and efficient.
- Do not overload responses with unrelated files or context.
- Prioritize accuracy and clarity.

## File splitting guidelines
- Split the work into separate logical areas whenever possible.
- `Controllers` and `Repositories` are detailed areas and should each get their own dedicated treatment.
- Do not combine `Controllers` and `Repositories` into a single file or single context block when asking follow-up questions.
- If a task involves `Controllers`, keep the relevant controller files isolated from repository logic.
- If a task involves `Repositories`, keep the relevant repository files isolated from controller logic.

## Recommended structure for AI prompts
Use a structure like this when requesting work:
1. Describe the goal clearly.
2. Provide the relevant folder or file scope.
3. Indicate whether the task is about `Controllers`, `Repositories`, or another layer.
4. Ask the AI to keep unrelated areas separate to save tokens.

## Example prompt format
- "Please inspect `ChineseRaffleApi/Controllers` and suggest fixes for controller routing."
- "Please inspect `ChineseRaffleApi/Repository` and suggest fixes for data access logic."
- "Keep controller and repository logic in separate review sections."

## Review requirement
- After generating a result, review it to make sure you understand everything.
- Confirm the suggestions are correct and that the split between controllers and repositories is preserved.
