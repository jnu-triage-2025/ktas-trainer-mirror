# Camera TODO

- [x] 카메라 홀더에 카메라를 부착하는 시스템을 CameraAttachPoint(플레이어 측) / CameraHolder(카메라 래퍼)로 분리
    - `CameraAttachPoint`: 플레이어 자식으로 생성되는 부착 지점. 상하 시야각(pitch) 피벗 + 뷰모델/아이템 부착점 담당.
      (이전의 "CameraHolder" GameObject를 대체)
    - `CameraHolder`: 실제 카메라를 감싸고 특정 CameraAttachPoint에 AttachTo/Detach 로 부착되어 추종하는 래퍼.
      거리/충돌/시점 모드 로직을 담당. `MainCameraController`가 이를 소유하고 API를 위임.
- [ ] 관전자 위치 표시 구현
    아이디어 배경: 레퍼런스로 잡은 마인크래프트에서 관전자 모드에서는 관전자의 위치도 대략적으로 표시함. 이 내용을 구현하는 데 있어, 카메라 자체에 이 표시를 추가하도록 해도 되나, 이것은 이후에 관련한 처리를 복잡하게 만들 우려가 있음. 그래서 CameraAttachPoint / CameraHolder 분리를 선행했으며, 이제 관전자 위치 표시는 카메라 래퍼(CameraHolder)가 아니라 부착점(CameraAttachPoint) 측 표현으로 추가하면 된다.
