Shader "PupilReflex/CorneaProjector"
{
  Properties
  {
    _LightColor ("Light Color", Color) = (1, 0.72, 0.26, 0.34)
    _ProjectorCenterWS ("Projector Center WS", Vector) = (0, 0, 0, 1)
    _AxisDirectionWS ("Axis Direction WS", Vector) = (0, 0, 1, 0)
    _LightPosWS ("Light Position WS", Vector) = (0, 0, 0, 1)
    _ProjectorRadius ("Projector Radius", Float) = 0.01
    _CorneaIOR ("Cornea IOR", Float) = 1.376
    _BaseSpecular ("Base Specular", Range(0,1)) = 0.35
    _StretchStrength ("Stretch Strength", Float) = 1.6
    _EdgeSoftness ("Edge Softness", Range(0.01, 1)) = 0.25
  }

  SubShader
  {
    Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
    Blend One One
    ZWrite Off
    ZTest Always
    Cull Off

    Pass
    {
      HLSLPROGRAM
      #pragma vertex vert
      #pragma fragment frag
      #include "UnityCG.cginc"

      struct appdata
      {
        float4 vertex : POSITION;
        float3 normal : NORMAL;
      };

      struct v2f
      {
        float4 pos : SV_POSITION;
        float3 worldPos : TEXCOORD0;
        float3 worldNormal : TEXCOORD1;
      };

      float4 _LightColor;
      float4 _ProjectorCenterWS;
      float4 _AxisDirectionWS;
      float4 _LightPosWS;
      float _ProjectorRadius;
      float _CorneaIOR;
      float _BaseSpecular;
      float _StretchStrength;
      float _EdgeSoftness;

      v2f vert(appdata v)
      {
        v2f o;
        o.pos = UnityObjectToClipPos(v.vertex);
        o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
        o.worldNormal = UnityObjectToWorldNormal(v.normal);
        return o;
      }

      float3 FresnelSchlick(float cosTheta, float3 F0)
      {
        return F0 + (1.0 - F0) * pow(1.0 - cosTheta, 5.0);
      }

      float ProjectorMask(float3 P, float3 N, float3 V, float3 L)
      {
        float viewGrazing = 1.0 - saturate(dot(N, V));
        float lightGrazing = 1.0 - saturate(dot(N, L));
        float stretch = 1.0 + _StretchStrength * (viewGrazing + lightGrazing);

        float3 up = abs(N.y) < 0.999 ? float3(0,1,0) : float3(1,0,0);
        float3 T = normalize(cross(up, N));
        float3 B = normalize(cross(N, T));

        float3 viewTangent = normalize(V - N * dot(V, N));
        if (length(viewTangent) < 0.0001)
        {
          viewTangent = T;
        }

        float3 majorAxis = normalize(viewTangent);
        float3 minorAxis = normalize(cross(N, majorAxis));

        float3 d = P - _ProjectorCenterWS.xyz;
        float u = dot(d, majorAxis);
        float v = dot(d, minorAxis);

        float a = _ProjectorRadius * stretch;
        float b = _ProjectorRadius;
        float ellipse = (u * u) / (a * a) + (v * v) / (b * b);

        float t = 1.0 - ellipse;
        return smoothstep(0.0, _EdgeSoftness, t);
      }

      float4 frag(v2f i) : SV_Target
      {
        float3 N = normalize(i.worldNormal);
        float3 V = normalize(_WorldSpaceCameraPos.xyz - i.worldPos);
        float3 L = normalize(_LightPosWS.xyz - i.worldPos);

        float NdotL = saturate(dot(N, L));
        float NdotV = saturate(dot(N, V));

        float F0s = pow((_CorneaIOR - 1.0) / (_CorneaIOR + 1.0), 2.0);
        float3 F0 = float3(F0s, F0s, F0s);
        float3 F = FresnelSchlick(NdotV, F0);

        float3 H = normalize(L + V);
        float NdotH = saturate(dot(N, H));
        float spec = pow(NdotH, 128.0) * _BaseSpecular;

        float mask = ProjectorMask(i.worldPos, N, V, L);

        float3 diffuse = _LightColor.rgb * NdotL * 0.05;
        float3 specular = _LightColor.rgb * spec * F;
        float3 projector = _LightColor.rgb * mask;

        float3 color = diffuse + specular + projector;

        return float4(color * _LightColor.a, 1.0);
      }
      ENDHLSL
    }
  }
}
