#!/bin/sh

set -eu

command -v rsvg-convert >/dev/null 2>&1 || {
	echo "rsvg-convert is required to render Lucide SVG strokes correctly." >&2
	exit 1
}

find ./Icons-Lucide -name "*.svg" -exec sh -c '
	for input_path do
		relative_path=${input_path#./Icons-Lucide/}
		output_path=./Icons/${relative_path%.svg}.png
		mkdir -p "$(dirname "$output_path")"
		rsvg-convert -o "$output_path" "$input_path"
	done
' sh {} +
