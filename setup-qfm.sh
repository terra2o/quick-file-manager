#!/bin/bash
set -e

PROJECT_DIR="$(pwd)"        # Project root
INSTALL_DIR="$HOME/bin"     # Installation directory
CONFIG_DIR="$HOME/.config/qfm"  # Config directory
EXEC_NAME="qfm"             # Final executable name
PROJECT_NAME="QuickFileManager"

echo "Building self-contained qfm executable..."

# Create install directory if missing
mkdir -p "$INSTALL_DIR"
# Create config directory if missing
mkdir -p "$CONFIG_DIR"

# Publish self-contained binary
dotnet publish "$PROJECT_DIR/QuickFileManager.csproj" \
    -c Release \
    -r linux-x64 \
    --self-contained true \
    -o "$INSTALL_DIR"

# Rename the binary to 'qfm'
mv "$INSTALL_DIR/$PROJECT_NAME" "$INSTALL_DIR/$EXEC_NAME"

# Make it executable
chmod +x "$INSTALL_DIR/$EXEC_NAME"

if [ -f "$PROJECT_DIR/config.json" ]; then
    echo "Copying config.json to $CONFIG_DIR/"
    cp "$PROJECT_DIR/config.json" "$CONFIG_DIR/config.json"
    echo "Config file installed at: $CONFIG_DIR/config.json"
else
    echo "Warning: config.json not found in project directory ($PROJECT_DIR/)"
    echo "A default config will be created when you first run qfm"
fi

# Add INSTALL_DIR to PATH if not already present
if ! echo "$PATH" | grep -q "$INSTALL_DIR"; then
    echo "export PATH=\$HOME/bin:\$PATH" >> "$HOME/.bashrc"
    source "$HOME/.bashrc"
fi

echo "Installation complete!"
echo "You can now run 'qfm' from any terminal."
echo "Config file location: $CONFIG_DIR/config.json"