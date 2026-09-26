#!/bin/zsh
# Publish the trainer and put a `drill` command on PATH. Re-run after changing the code.
set -euo pipefail
HERE="$(cd "$(dirname "$0")/.." && pwd)"
OUT="$HOME/.local/share/drill"
mkdir -p "$OUT" "$HOME/.local/bin"
dotnet publish "$HERE/src/Drill/Drill.csproj" -c Release -o "$OUT/bin" --nologo -v quiet
cat > "$HOME/.local/bin/drill" <<SHIM
#!/bin/zsh
export DRILL_HOME="\${DRILL_HOME:-$HERE}"
exec dotnet "$OUT/bin/drill.dll" "\$@"
SHIM
chmod +x "$HOME/.local/bin/drill"
echo "installed: $(command -v drill || echo "$HOME/.local/bin/drill") (DRILL_HOME=$HERE)"
