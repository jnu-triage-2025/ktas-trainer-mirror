# Textures

## Icons / Icons-Lucide

[Lucide](https://lucide.dev)

특별한 이유가 아니라면 통일성을 위해 Lucide 아이콘을 수정해 사용합니다. 우선 아이콘을 Lucide로부터 svg 파일로 가져와, stroke와 background(fill) 속성을 CSS `white` 값으로 수정합니다. 

다음은 수정된 message-circle.svg 파일에서 가져왔습니다:

```html
<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="white" stroke="white" stroke-width="2" stroke-linecap="round" 
```

그 뒤 lucide-convert.sh 스크립트를 사용해 Unity에서 사용할 수 있는 png 파일로 변환합니다. 이 과정에서 Icons-Lucide 폴더의 모든 SVG 파일이 Icons 폴더에 PNG로 변환됩니다.  

```bash
❯ pwd
(Project Path)/Assets/Modules/MultiplayerInfrastructure/Resources/Textures
❯ sh lucide-convert.sh
```
