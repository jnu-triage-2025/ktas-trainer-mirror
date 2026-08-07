#!/bin/sh
# unity-merge 준비 스크립트 (macOS/Linux).
#
# 실행 (working directory: 저장소 루트 또는 어디든):
#   ./Tools/setup-unity-merge.sh
#
# 수행 내용:
#   1. Tools/unity-merge 서브모듈 초기화/갱신
#   2. unity-merge 빌드 (Tools/unity-merge/setup.sh에 위임; cmake/C++ 툴체인이
#      없으면 설치 시도 후 실패 시 안내하고 종료 코드 1)
#   3. 이 저장소 로컬에만 git 설정 연결:
#      - core.hooksPath=.githooks  (커밋 직전 .unity-merge/INDEX 갱신 훅)
#      - include.path=../.gitconfig (unity-yaml merge driver 등록)
#
# 종료 코드: 0 성공, 0 이외 실패.
set -eu

cd "$(dirname "$0")/.."

git submodule update --init Tools/unity-merge
Tools/unity-merge/setup.sh
git config core.hooksPath .githooks
git config include.path ../.gitconfig

echo "unity-merge 준비 완료: unity-yaml merge driver 및 pre-commit 인덱스 훅이 이 저장소에 활성화되었습니다."
