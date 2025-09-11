#!/bin/bash
set -e

PROJECT_DIR="$(pwd)"        # Project root
INSTALL_DIR="$HOME/bin"     # Installation directory
EXEC_NAME="qfm"             # Final executable name
PROJECT_NAME="QuickFileManager"

echo "Building self-contained qfm executable..."

# Creates install directory if missing
mkdir -p "$INSTALL_DIR"

# Publish self-contained binary
dotnet publish "$PROJECT_DIR/QuickFileManager.csproj" \
    -c Release \
    -r linux-x64 \
    --self-contained true \
    -o "$INSTALL_DIR"

# Renames the binary to 'qfm'
mv "$INSTALL_DIR/$PROJECT_NAME" "$INSTALL_DIR/$EXEC_NAME"

# Makes it executable
chmod +x "$INSTALL_DIR/$EXEC_NAME"

# Adds INSTALL_DIR to PATH if not already present
if ! echo "$PATH" | grep -q "$INSTALL_DIR"; then
    echo "export PATH=\$HOME/bin:\$PATH" >> "$HOME/.bashrc"
    source "$HOME/.bashrc"
fi

echo "Installation complete!"
echo "You can now run 'qfm' from any terminal."
