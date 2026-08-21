# Code Mirror

GitLab `origin`의 원격 추적 브랜치 또는 현재 로컬 Git 저장소의 브랜치와 태그를 일반 Git 대상 저장소로 변환합니다. GitHub, GitLab, 자체 호스팅 Git 서버, 로컬 bare 저장소 경로 등 `git push`가 가능한 모든 Git 원격 저장소에 사용할 수 있습니다. 작업 디렉터리는 읽지 않으므로, 미커밋 변경은 대상에 포함되지 않습니다.

각 원본 커밋의 전체 트리에서 허용 목록에 없는 파일과 활성 제외 규칙에 맞는 파일을 제거한 뒤, 새 Git 커밋을 만듭니다. 커밋 메시지, 작성자와 커미터의 이름·전자 메일·시각, 부모 관계와 병합 커밋을 보존합니다. 필터링 때문에 원본 커밋 해시는 보존할 수 없습니다.

## 준비

`code-mirror.toml`의 `destination.url`을 대상 Git 저장소 URL로 변경합니다. SSH URL은 해당 키에 쓰기 권한이 있으면 토큰이 필요하지 않습니다. HTTPS 인증이 필요한 경우 `Tools/code-mirror/.env`에서 `token_env`에 지정한 환경 변수와 `http_username`을 설정합니다. 제공자마다 HTTP 사용자명이 다를 수 있으므로, 해당 Git 서버의 토큰 인증 방식을 확인해야 합니다. `.env`와 상태 파일은 Git에서 제외됩니다.

`[source]`의 `mode`는 원본 참조 범위를 선택합니다. 기본 `remote_tracking`은 이미 로컬에 fetch된 `origin` 원격 추적 브랜치와 로컬 태그를 읽고, `local`은 현재 저장소의 로컬 브랜치와 로컬 태그를 읽습니다. 따라서 원격이 없는 로컬 Git 저장소도 `mode = "local"`로 처리할 수 있습니다. 대상이 로컬 bare 저장소라면 `destination.url`에 절대 경로를 넣으면 됩니다. `remote_tracking`을 지속 동기화할 때에는 실행 전에 별도로 `git fetch origin`을 수행해야 합니다.

```sh
cd /path/to/ktas-trainer
python3 Tools/code-mirror/code_mirror.py --config Tools/code-mirror/code-mirror.toml --dry-run
python3 Tools/code-mirror/code_mirror.py --config Tools/code-mirror/code-mirror.toml --generate-config
python3 Tools/code-mirror/code_mirror.py --config Tools/code-mirror/code-mirror.toml
python3 Tools/code-mirror/code_mirror.py --config Tools/code-mirror/code-mirror.toml --push
```

첫 번째 명령은 제외 결과와 변환 대상 커밋 수만 확인합니다. 두 번째 명령은 원본 커밋 해시와 변환된 커밋 해시의 매핑을 `.code-mirror-state.json`에 저장합니다. 세 번째 명령은 상태 저장 후 대상 Git 원격 저장소에 push합니다. `--dry-run`도 Git 객체를 계산하기 위해 로컬 객체 데이터베이스에 도달 불가능 객체를 만들 수는 있지만, 참조·상태 파일·원격 저장소는 변경하지 않습니다.

## 규칙과 범위

`[include]`는 기본 허용 목록입니다. 코드와 텍스트 중심의 확장자만 기본 포함하므로, 모델·텍스처·오디오처럼 작은 콘텐츠 파일도 기본적으로 제외됩니다. 특정 텍스트 데이터 파일을 포함해야 하면 `extensions`, `file_names`, `paths`에 추가합니다.

`[[rules]]`는 허용 목록을 통과한 파일을 추가로 제외합니다. `match`에는 `max_size_bytes`, `directory_names`, `file_names`, `extensions`, `paths`를 여러 개 지정할 수 있으며, 하나라도 일치하면 제외합니다. 50 MiB 이상의 파일을 제외하는 규칙이 기본으로 포함됩니다.

## 모듈 정책과 생성 설정

`[module_policies]`는 `Assets/Modules/<모듈명>/` 전체에 적용하는 최우선 수동 정책입니다. `FishNet`, `MultiplayerInfrastructure`, `TriageTrainer`, `TextToSpeechService`는 PNG 등의 콘텐츠 확장자와 파일 크기를 포함하여 항상 미러링합니다. 지정된 라이선스 콘텐츠 모듈은 항상 제외하되, Unity 참조 연결에 필요한 `.meta` 파일은 포함합니다.

`--generate-config`는 현재 작업 트리의 `Assets/Modules/*/package.json`을 읽어 `unity` 필드가 있고 `license`가 정확히 `MIT`인 Unity 패키지를 찾아 `code-mirror.gen.toml`을 생성합니다. 이 파일이 추가하는 `always_include` 정책은 수동 `code-mirror.toml`보다 낮은 우선순위를 가지므로, 수동 포함·제외 목록에 적힌 모듈은 자동 분류 결과와 관계없이 수동 설정으로 제어됩니다. 생성 파일은 로컬 산출물이며 Git에서 제외됩니다.

규칙의 `scope.mode`는 다음과 같습니다.

- `always`: 모든 원본 커밋에 적용합니다.
- `branches`: 지정 브랜치 팁에서 도달 가능한 원본 커밋에 적용합니다. 병합으로 여러 브랜치에서 도달 가능한 커밋은 해당 브랜치 규칙을 모두 적용합니다.
- `before`: 지정 커밋과 그 조상에 적용합니다.
- `after`: 지정 커밋과 그 후손에 적용합니다.
- `between`: `from` 커밋과 그 후손이면서 `to` 커밋과 그 조상인 커밋에 적용합니다.

범위의 커밋 해시는 모두 GitLab 원본 커밋 해시입니다. `branches.names`에는 현재 구현상 정확한 브랜치명을 사용해야 합니다.

## 지속 동기화와 상태

상태 파일은 원본 커밋 해시 → 미러 커밋 해시 매핑, 마지막으로 확인한 원본 참조, 마지막 push 결과, 필터링 설정 지문을 직렬화합니다. 같은 설정으로 다시 실행하면 기존 객체를 재사용하며 새 원본 커밋과 새 참조를 반영합니다. 원본의 강제 push나 새 브랜치 때문에 브랜치 범위 규칙의 결과가 달라질 수 있으므로, 매 실행마다 전체 원본 그래프를 다시 판정합니다. 대상 참조가 상태 파일에 기록된 값과 다르면 외부 변경으로 보고 push를 거부합니다.

필터링 설정이 바뀌면 대상 이력도 달라질 수 있습니다. 이때 도구는 중단하며, `--dry-run --rebuild`로 결과를 검토한 뒤 `--rebuild --push`를 명시해야 합니다. 삭제된 원본 브랜치·태그는 안전을 위해 대상 원격 저장소에서 자동 삭제하지 않습니다.

## 한계

Git 서명, 원본 커밋 해시, Git LFS 객체 자체는 복제하지 않습니다. 원본 annotated tag는 변환된 커밋을 가리키는 lightweight tag가 되므로 태그 메시지와 서명도 보존하지 않습니다. 서브모듈 gitlink와 심볼릭 링크도 기본 허용 목록에 포함되지 않습니다. 필요하면 `paths` 허용 목록으로 검토 후 추가할 수 있습니다.
