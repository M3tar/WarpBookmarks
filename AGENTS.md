# Workspace Rules

- Treat the repository root as the only canonical project root.
- Create and update all source code, documentation, tests, build artifacts, and release packages inside this project root.
- Store test and release instructions in `docs/`.
- Store test and build scripts in `scripts/`.
- Store ZIP archives and release packages in `dist/`.
- Do not copy project files outside the repository unless the user explicitly requests an exact destination.
- When returning a downloadable local artifact, link directly to the file under this project root instead of creating a duplicate elsewhere.
