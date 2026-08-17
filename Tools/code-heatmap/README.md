# Code Heatmap

독립적인 uv 프로젝트입니다. Unity 또는 서드파티 Python 패키지에 의존하지 않고, 소스 파일의 텍스트량을 폴더 트리맵 SVG로 렌더합니다.

저장소 루트에서 실행합니다.

```sh
uv run --project Tools/code-heatmap code-heatmap --output /tmp/code-heatmap.svg
uv run --project Tools/code-heatmap code-heatmap --refs main,HEAD~10,v1.0 --color-by module --output /tmp/history.svg
```

테스트:

```sh
uv run --project Tools/code-heatmap python -m unittest discover -s Tools/code-heatmap/tests
```

자세한 옵션은 상위 [Tools README](../README.md)를 참고합니다.
