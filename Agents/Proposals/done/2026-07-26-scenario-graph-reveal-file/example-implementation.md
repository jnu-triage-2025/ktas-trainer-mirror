# Scenario Graph Reveal File — Example Implementation

```text
label = PlatformSpecificRevealLabel(Application.platform)

if currentFilePath exists:
    File menu adds enabled item(label)
    on click:
        EditorUtility.RevealInFinder(absolute currentFilePath)
else:
    File menu adds disabled item(label)
```

플랫폼별 문구는 macOS `Reveal in Finder`, Windows `Show in File Explorer`, Linux `Show in File Manager`를 사용한다.
