# <a id="MultiplayerInfrastructure_Permission"></a> Namespace MultiplayerInfrastructure.Permission

### Classes

 [PermissionService](MultiplayerInfrastructure.Permission.PermissionService.md)

커맨드 권한 시스템.

permissions.json 파일을 서버 측에서 읽고 쓴다.
파일이 없으면 기본값으로 자동 생성한다.

## 구조
  roles        : 정의된 모든 role 이름 목록
  default      : 새 유저에게 자동 부여되는 role 이름
  permissions  : role 별 설정
    [roleName].contains    : 이 role이 상속할 다른 role 목록
    [roleName].permissions : 이 role에 직접 부여된 permission identifier 목록
  userRoles    : userIdentifier → roleName 매핑 (런타임 영속)

## Permission identifier 형식
  "command"          — 특정 최상위 커맨드
  "command.sub"      — 특정 서브커맨드
  "*"                — 모든 커맨드
  "command.*"        — command 하위 모든 identifier

## 기본 role
  user     : 대부분의 커맨드 사용 가능 (permission 관리 제외)
  operator : user 상속 + permission 관리 커맨드 사용 가능

