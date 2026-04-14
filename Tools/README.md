# Tools

이 폴더는 프로젝트 운영/검증/유지보수를 위한 보조 도구 스크립트를 모아두는 위치입니다.

기본 원칙:
- 가능하면 독립 실행 가능(단일 파일)로 유지합니다.
- 실행 위치와 인자 사용법을 파일 상단 주석에 명시합니다.
- 실패 시 종료 코드(0 성공, 0 이외 실패)를 명확히 반환합니다.

## 현재 도구

### check-markdown-links.sh

Markdown 문서의 링크 대상 파일 존재 여부를 검사합니다.

실행 예시 (working directory: Tools):
- `./check-markdown-links.sh`
- `./check-markdown-links.sh ../Documents`
- `./check-markdown-links.sh ../Documents/README.md ../Documents/working-guide`

### unitydiff
