@echo off
rem unity-merge 준비 스크립트 (Windows).
rem
rem 실행 (working directory: 저장소 루트 또는 어디든):
rem   Tools\setup-unity-merge.bat
rem
rem 수행 내용:
rem   1. Tools/unity-merge 서브모듈 초기화/갱신
rem   2. unity-merge 빌드 (Tools\unity-merge\setup.bat에 위임; cmake/C++ 툴체인이
rem      없으면 설치 시도 후 실패 시 안내하고 종료 코드 1)
rem   3. 이 저장소 로컬에만 git 설정 연결:
rem      - core.hooksPath=.githooks  (커밋 직전 .unity-merge/INDEX 갱신 훅)
rem      - include.path=../.gitconfig (unity-merge merge driver 등록)
rem
rem 종료 코드: 0 성공, 0 이외 실패.
setlocal
cd /d "%~dp0\.."

git submodule update --init Tools/unity-merge
if errorlevel 1 exit /b 1
call Tools\unity-merge\setup.bat
if errorlevel 1 exit /b 1
git config core.hooksPath .githooks
if errorlevel 1 exit /b 1
git config include.path ../.gitconfig
if errorlevel 1 exit /b 1

echo unity-merge 준비 완료: unity-merge merge driver 및 pre-commit 인덱스 훅이 이 저장소에 활성화되었습니다.
endlocal
