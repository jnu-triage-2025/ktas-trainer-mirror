find ./Icons-Lucide -name "*.svg" -exec mogrify -density 300 -background none -path ./Icons -format png {} \;
