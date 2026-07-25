# AI Skills

AI는 작업 목표를 달성하기 위해 기본적인 기능 외에도 다양한 임시 스크립트를 즉석으로 작성해 사용할 수 있습니다. 이러한 스크립트는 대부분 개발 환경에는 널리 깔려있는 프로그램이나 컴파일러를 사용하지만, 어떤 기술 스택을 사용할 지는 랜덤에 가깝습니다.  

사용자의 컴퓨터가 임시 스크립트를 실행할 수 있는 환경을 갖추지 못하고 있다면, AI는 다른 방법을 찾으려고 시도하지만, 이 과정에서 추가적인 시간과 토큰이 소모되거나 원래의 의도한 방향과는 조금 다른 스크립트가 생성되어 정확도가 떨어질 수 있습니다.  

```shell
fd -euss -euss-template 'title' 2>/dev/null; fd 'title' -e uss -e tssAssets 2>/dev/null; rg -l "title-ui__actionbar\|title-ui__title\|actionbar-label" --glob '*.uss' -N
zsh:1: command not found: rg
```

따라서 다음의 기술 스택은 갖추는 것을 권합니다.  

| 스택 | 설치 방법(macOS) | 설명 |
| :-: | :-: | :-- |
| Apple Command Line Tools | `xcode-select --install` | 기본 개발/명령어 도구(`sh`, `zsh`, `bash`, `python`, `ruby`, ...) |
| `rg` | `brew install ripgrep` | Ripgrep: 파일 내용 검색 |
| `fd` | `brew install fd` | fd: 파일 이름 검색 |
| `tree` | `brew install tree` | tree: 디렉토리 구조 시각화 |
| `ast-grep` | `brew install ast-grep` | AST Grep: 소스코드 구조 검색 |
| `yq` | `brew install yq` | yq: YAML/JSON 구조 검색 |
| `wget` | `brew install wget` | wget: 파일 다운로드 |
| `node` | `brew install node` | Node.js: JavaScript(`.js` 파일) 런타임 |
| `dotnet` | `brew install dotnet` | .NET: C#(`.cs` 파일) 런타임 |
