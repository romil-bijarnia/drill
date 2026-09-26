#!/bin/zsh
# Publish the trainer and put a `reps` command on PATH. Re-run after changing the code.
set -euo pipefail
HERE="$(cd "$(dirname "$0")/.." && pwd)"
OUT="$HOME/.local/share/reps"
mkdir -p "$OUT" "$HOME/.local/bin"
dotnet publish "$HERE/src/Reps/Reps.csproj" -c Release -o "$OUT/bin" --nologo -v quiet
cat > "$HOME/.local/bin/reps" <<SHIM
#!/bin/zsh
export REPS_HOME="\${REPS_HOME:-$HERE}"
exec dotnet "$OUT/bin/reps.dll" "\$@"
SHIM
chmod +x "$HOME/.local/bin/reps"
echo "installed: $(command -v reps || echo "$HOME/.local/bin/reps") (REPS_HOME=$HERE)"
