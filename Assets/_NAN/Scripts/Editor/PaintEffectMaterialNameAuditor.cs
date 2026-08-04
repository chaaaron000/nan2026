using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Nan.Editor
{
    /// <summary>
    /// 물감 이펙트 프리팹의 파티클 렌더러 이름과 머티리얼 역할 매핑을 검사하고 표준 이름으로 정리한다.
    /// </summary>
    public static class PaintEffectMaterialNameAuditor
    {
        private static readonly string[] PrefabRoots =
        {
            "Assets/_NAN/Arts/Effect/Prefab",
            "Assets/_NAN/Arts/Object/Bucket/Prefab",
        };

        private static readonly Dictionary<string, string> MaterialNameToRendererName = new()
        {
            { "_center_", "paint" },
            { "_edge_", "Paint" },
            { "_bubble_", "bubble" },
            { "_glowSub_", "glowSub" },
            { "_glow_", "glow" },
        };

        /// <summary>
        /// 모든 물감 center/edge 이펙트 프리팹의 렌더러 이름, 머티리얼, 기대 이름을 문자열로 반환한다.
        /// </summary>
        public static string Audit()
        {
            StringBuilder builder = new();

            foreach (string path in GetAuditedPrefabPaths())
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null)
                {
                    continue;
                }

                foreach (Renderer renderer in prefab.GetComponentsInChildren<Renderer>(true))
                {
                    string materialName = renderer.sharedMaterial == null
                        ? string.Empty
                        : renderer.sharedMaterial.name;
                    string expectedName = GetExpectedRendererName(materialName);
                    string status = string.IsNullOrEmpty(expectedName)
                        ? "UNKNOWN"
                        : renderer.gameObject.name == expectedName
                            ? "OK"
                            : "RENAME";

                    builder
                        .Append(status)
                        .Append(" | ")
                        .Append(path)
                        .Append(" | ")
                        .Append(renderer.gameObject.name)
                        .Append(" | ")
                        .Append(materialName)
                        .Append(" | expected=")
                        .Append(expectedName)
                        .AppendLine();
                }
            }

            return builder.ToString();
        }

        /// <summary>
        /// 머티리얼 이름으로 역할을 판별할 수 있는 모든 물감 이펙트 렌더러 GameObject를 표준 이름으로 변경한다.
        /// </summary>
        public static string NormalizeRendererNames()
        {
            StringBuilder builder = new();
            int changedCount = 0;

            foreach (string path in GetAuditedPrefabPaths())
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null)
                {
                    continue;
                }

                bool changed = false;
                foreach (Renderer renderer in prefab.GetComponentsInChildren<Renderer>(true))
                {
                    string materialName = renderer.sharedMaterial == null
                        ? string.Empty
                        : renderer.sharedMaterial.name;
                    string expectedName = GetExpectedRendererName(materialName);
                    if (string.IsNullOrEmpty(expectedName)
                        || renderer.gameObject.name == expectedName)
                    {
                        continue;
                    }

                    builder
                        .Append(path)
                        .Append(" | ")
                        .Append(renderer.gameObject.name)
                        .Append(" -> ")
                        .Append(expectedName)
                        .Append(" | ")
                        .Append(materialName)
                        .AppendLine();

                    renderer.gameObject.name = expectedName;
                    changed = true;
                    changedCount++;
                }

                if (changed)
                {
                    EditorUtility.SetDirty(prefab);
                    PrefabUtility.SavePrefabAsset(prefab);
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            builder.Insert(0, $"Changed renderers: {changedCount}{Environment.NewLine}");
            return builder.ToString();
        }

        /// <summary>
        /// 모든 PaintVisualSet 에셋의 이펙트 머티리얼 참조가 에셋 이름의 팔레트, 색상, 역할 규칙과 맞는지 반환한다.
        /// </summary>
        public static string AuditVisualSetMaterials()
        {
            StringBuilder builder = new();

            foreach (string path in GetPaintVisualSetPaths())
            {
                PaintVisualSet visualSet =
                    AssetDatabase.LoadAssetAtPath<PaintVisualSet>(path);
                if (visualSet == null)
                {
                    continue;
                }

                string assetName = visualSet.name;
                string paletteSuffix = GetPaletteSuffix(assetName);
                string colorSuffix = GetColorSuffix(assetName);
                SerializedObject serializedObject = new(visualSet);

                AppendVisualSetMaterialStatus(
                    builder,
                    path,
                    serializedObject,
                    "centerMaterial",
                    "center",
                    colorSuffix,
                    paletteSuffix);
                AppendVisualSetMaterialStatus(
                    builder,
                    path,
                    serializedObject,
                    "edgeMaterial",
                    "edge",
                    colorSuffix,
                    paletteSuffix);
                AppendVisualSetMaterialStatus(
                    builder,
                    path,
                    serializedObject,
                    "bubbleMaterial",
                    "bubble",
                    colorSuffix,
                    paletteSuffix);
                AppendVisualSetMaterialStatus(
                    builder,
                    path,
                    serializedObject,
                    "glowMaterial",
                    "glow",
                    colorSuffix,
                    paletteSuffix);
                AppendVisualSetMaterialStatus(
                    builder,
                    path,
                    serializedObject,
                    "glowSubMaterial",
                    "glowSub",
                    colorSuffix,
                    paletteSuffix);
            }

            return builder.ToString();
        }

        /// <summary>
        /// center와 edge 역할 파티클의 시작 색상 RGB가 머티리얼 색을 방해하지 않는 흰색인지 검사한다.
        /// </summary>
        public static string AuditRoleParticleTints()
        {
            StringBuilder builder = new();

            foreach (string path in GetAuditedPrefabPaths())
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null)
                {
                    continue;
                }

                foreach (ParticleSystem particleSystem in GetRoleParticleSystems(prefab))
                {
                    ParticleSystem.MinMaxGradient startColor = particleSystem.main.startColor;
                    builder
                        .Append(IsWhiteTint(startColor) ? "OK" : "TINTED")
                        .Append(" | ")
                        .Append(path)
                        .Append(" | ")
                        .Append(particleSystem.gameObject.name)
                        .Append(" | mode=")
                        .Append(startColor.mode)
                        .Append(" | min=")
                        .Append(startColor.colorMin)
                        .Append(" | max=")
                        .Append(startColor.colorMax)
                        .AppendLine();
                }
            }

            return builder.ToString();
        }

        /// <summary>
        /// center와 edge 역할 파티클의 시작 색상 RGB를 흰색으로 바꾸고 기존 알파와 그라디언트 시간을 보존한다.
        /// </summary>
        public static string NormalizeRoleParticleTints()
        {
            StringBuilder builder = new();
            int changedCount = 0;

            foreach (string path in GetAuditedPrefabPaths())
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null)
                {
                    continue;
                }

                bool changed = false;
                foreach (ParticleSystem particleSystem in GetRoleParticleSystems(prefab))
                {
                    ParticleSystem.MainModule main = particleSystem.main;
                    ParticleSystem.MinMaxGradient startColor = main.startColor;
                    if (IsWhiteTint(startColor))
                    {
                        continue;
                    }

                    main.startColor = CreateWhiteTint(startColor);
                    builder
                        .Append(path)
                        .Append(" | ")
                        .Append(particleSystem.gameObject.name)
                        .Append(" | ")
                        .Append(startColor.colorMax)
                        .Append(" -> white RGB")
                        .AppendLine();
                    changed = true;
                    changedCount++;
                }

                if (changed)
                {
                    EditorUtility.SetDirty(prefab);
                    PrefabUtility.SavePrefabAsset(prefab);
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            builder.Insert(0, $"Changed particle systems: {changedCount}{Environment.NewLine}");
            return builder.ToString();
        }

        /// <summary>
        /// 빨간 center와 edge 프리팹 인스턴스에 적록 보정 VisualSet을 적용한 최종 머티리얼과 파티클 틴트를 검사한다.
        /// </summary>
        public static string ProbeRedGreenRedEffectApplication()
        {
            const string visualSetPath =
                "Assets/_NAN/ScriptableObjects/ColorPalettes/RedGreenRedVisualSet.asset";
            string[] prefabPaths =
            {
                "Assets/_NAN/Arts/Effect/Prefab/Red/Paint_Center_Red.prefab",
                "Assets/_NAN/Arts/Effect/Prefab/Red/Paint_Edge_Red.prefab",
            };

            PaintVisualSet visualSet =
                AssetDatabase.LoadAssetAtPath<PaintVisualSet>(visualSetPath);
            PaintEffectLibrary effectLibrary = new();
            StringBuilder builder = new();

            foreach (string prefabPath in prefabPaths)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                GameObject instance = UnityEngine.Object.Instantiate(prefab);
                try
                {
                    effectLibrary.ApplyVisualSet(instance, visualSet);
                    foreach (ParticleSystem particleSystem in GetRoleParticleSystems(instance))
                    {
                        ParticleSystemRenderer renderer =
                            particleSystem.GetComponent<ParticleSystemRenderer>();
                        Material material = renderer.sharedMaterial;
                        bool materialMatches = material != null
                                               && material.name.EndsWith("_R_RG", StringComparison.Ordinal);
                        bool tintMatches = IsWhiteTint(particleSystem.main.startColor);

                        builder
                            .Append(materialMatches && tintMatches ? "OK" : "FAILED")
                            .Append(" | ")
                            .Append(prefabPath)
                            .Append(" | ")
                            .Append(particleSystem.gameObject.name)
                            .Append(" | material=")
                            .Append(material == null ? "null" : material.name)
                            .Append(" | whiteTint=")
                            .Append(tintMatches)
                            .AppendLine();
                    }
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(instance);
                }
            }

            return builder.ToString();
        }

        private static IEnumerable<string> GetAuditedPrefabPaths()
        {
            string[] guids = AssetDatabase.FindAssets("t:Prefab", PrefabRoots);
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (IsAuditedPrefabPath(path))
                {
                    yield return path;
                }
            }
        }

        private static bool IsAuditedPrefabPath(string path)
        {
            return path.Contains("Paint_Center_", StringComparison.Ordinal)
                   || path.Contains("Paint_Edge_", StringComparison.Ordinal)
                   || path.Contains("PaintBucket_", StringComparison.Ordinal);
        }

        private static string GetExpectedRendererName(string materialName)
        {
            if (string.IsNullOrWhiteSpace(materialName))
            {
                return string.Empty;
            }

            foreach (KeyValuePair<string, string> pair in MaterialNameToRendererName)
            {
                if (materialName.Contains(pair.Key, StringComparison.OrdinalIgnoreCase))
                {
                    return pair.Value;
                }
            }

            return string.Empty;
        }

        private static IEnumerable<string> GetPaintVisualSetPaths()
        {
            string[] guids = AssetDatabase.FindAssets(
                "t:PaintVisualSet",
                new[] { "Assets/_NAN/ScriptableObjects/ColorPalettes" });
            foreach (string guid in guids)
            {
                yield return AssetDatabase.GUIDToAssetPath(guid);
            }
        }

        private static IEnumerable<ParticleSystem> GetRoleParticleSystems(GameObject prefab)
        {
            foreach (ParticleSystemRenderer renderer in prefab.GetComponentsInChildren<ParticleSystemRenderer>(true))
            {
                if (renderer.gameObject.name != "paint" && renderer.gameObject.name != "Paint")
                {
                    continue;
                }

                ParticleSystem particleSystem = renderer.GetComponent<ParticleSystem>();
                if (particleSystem != null)
                {
                    yield return particleSystem;
                }
            }
        }

        private static bool IsWhiteTint(ParticleSystem.MinMaxGradient startColor)
        {
            return startColor.mode switch
            {
                ParticleSystemGradientMode.Color => IsWhiteRgb(startColor.color),
                ParticleSystemGradientMode.TwoColors =>
                    IsWhiteRgb(startColor.colorMin) && IsWhiteRgb(startColor.colorMax),
                ParticleSystemGradientMode.Gradient => IsWhiteGradient(startColor.gradient),
                ParticleSystemGradientMode.TwoGradients =>
                    IsWhiteGradient(startColor.gradientMin) && IsWhiteGradient(startColor.gradientMax),
                ParticleSystemGradientMode.RandomColor => IsWhiteGradient(startColor.gradient),
                _ => false,
            };
        }

        private static ParticleSystem.MinMaxGradient CreateWhiteTint(
            ParticleSystem.MinMaxGradient startColor)
        {
            return startColor.mode switch
            {
                ParticleSystemGradientMode.Color =>
                    new ParticleSystem.MinMaxGradient(ToWhiteRgb(startColor.color)),
                ParticleSystemGradientMode.TwoColors =>
                    new ParticleSystem.MinMaxGradient(
                        ToWhiteRgb(startColor.colorMin),
                        ToWhiteRgb(startColor.colorMax)),
                ParticleSystemGradientMode.Gradient =>
                    new ParticleSystem.MinMaxGradient(ToWhiteGradient(startColor.gradient)),
                ParticleSystemGradientMode.TwoGradients =>
                    new ParticleSystem.MinMaxGradient(
                        ToWhiteGradient(startColor.gradientMin),
                        ToWhiteGradient(startColor.gradientMax)),
                ParticleSystemGradientMode.RandomColor => CreateWhiteRandomColor(startColor.gradient),
                _ => startColor,
            };
        }

        private static ParticleSystem.MinMaxGradient CreateWhiteRandomColor(Gradient gradient)
        {
            ParticleSystem.MinMaxGradient result =
                new(ToWhiteGradient(gradient));
            result.mode = ParticleSystemGradientMode.RandomColor;
            return result;
        }

        private static bool IsWhiteGradient(Gradient gradient)
        {
            if (gradient == null)
            {
                return true;
            }

            foreach (GradientColorKey key in gradient.colorKeys)
            {
                if (!IsWhiteRgb(key.color))
                {
                    return false;
                }
            }

            return true;
        }

        private static Gradient ToWhiteGradient(Gradient source)
        {
            if (source == null)
            {
                return null;
            }

            GradientColorKey[] colorKeys = source.colorKeys;
            for (int index = 0; index < colorKeys.Length; index++)
            {
                colorKeys[index].color = Color.white;
            }

            Gradient result = new();
            result.SetKeys(colorKeys, source.alphaKeys);
            result.mode = source.mode;
            return result;
        }

        private static bool IsWhiteRgb(Color color)
        {
            return Mathf.Approximately(color.r, 1f)
                   && Mathf.Approximately(color.g, 1f)
                   && Mathf.Approximately(color.b, 1f);
        }

        private static Color ToWhiteRgb(Color color)
        {
            return new Color(1f, 1f, 1f, color.a);
        }

        private static void AppendVisualSetMaterialStatus(
            StringBuilder builder,
            string path,
            SerializedObject serializedObject,
            string propertyName,
            string roleName,
            string colorSuffix,
            string paletteSuffix)
        {
            SerializedProperty property =
                serializedObject.FindProperty(propertyName);
            Material material = property.objectReferenceValue as Material;
            string materialName = material == null
                ? string.Empty
                : material.name;
            string expectedName = string.IsNullOrEmpty(colorSuffix)
                ? string.Empty
                : $"M_paint_{roleName}_{colorSuffix}{paletteSuffix}";
            string status = materialName == expectedName
                ? "OK"
                : "MISMATCH";

            builder
                .Append(status)
                .Append(" | ")
                .Append(path)
                .Append(" | ")
                .Append(propertyName)
                .Append(" | ")
                .Append(materialName)
                .Append(" | expected=")
                .Append(expectedName)
                .AppendLine();
        }

        private static string GetPaletteSuffix(string assetName)
        {
            if (assetName.StartsWith("RedGreen", StringComparison.Ordinal))
            {
                return "_RG";
            }

            if (assetName.StartsWith("BlueYellow", StringComparison.Ordinal))
            {
                return "_BY";
            }

            return string.Empty;
        }

        private static string GetColorSuffix(string assetName)
        {
            if (assetName.Contains("RedVisualSet", StringComparison.Ordinal))
            {
                return "R";
            }

            if (assetName.Contains("GreenVisualSet", StringComparison.Ordinal))
            {
                return "G";
            }

            if (assetName.Contains("BlueVisualSet", StringComparison.Ordinal))
            {
                return "B";
            }

            if (assetName.Contains("YellowVisualSet", StringComparison.Ordinal))
            {
                return "Y";
            }

            if (assetName.Contains("CyanVisualSet", StringComparison.Ordinal))
            {
                return "C";
            }

            if (assetName.Contains("MagentaVisualSet", StringComparison.Ordinal))
            {
                return "M";
            }

            if (assetName.Contains("WhiteVisualSet", StringComparison.Ordinal))
            {
                return "W";
            }

            return string.Empty;
        }
    }
}
