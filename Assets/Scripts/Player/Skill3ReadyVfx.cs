using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 技能3可使用提示：觸發時爆出金色火花，待施放期間在玩家身邊持續環繞。
/// 粒子系統於執行時建立，不需要外部模型、貼圖或 Prefab。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(SkillSystem))]
public sealed class Skill3ReadyVfx : MonoBehaviour
{
    const float VFX_SCALE = 3f;
    static readonly Color ReadyGold = new Color(1f, 0.72f, 0.08f, 1f);
    static readonly Color ReadyYellow = new Color(1f, 0.95f, 0.35f, 1f);

    SkillSystem _skills;
    ParticleSystem _aura;
    ParticleSystem _burst;
    Material _particleMaterial;
    Texture2D _particleTexture;
    bool _wasReady;

    void Awake()
    {
        _skills = GetComponent<SkillSystem>();
        _particleTexture = CreateSoftParticleTexture();
        _particleMaterial = CreateParticleMaterial(_particleTexture);
        _aura = CreateAuraParticles();
        _burst = CreateBurstParticles();
    }

    void OnEnable()
    {
        if (_skills == null) _skills = GetComponent<SkillSystem>();
        _skills.OnStateChanged += RefreshState;
        _skills.OnSkill3Proc += HandleSkill3Proc;
        RefreshState();
    }

    void OnDisable()
    {
        if (_skills == null) return;
        _skills.OnStateChanged -= RefreshState;
        _skills.OnSkill3Proc -= HandleSkill3Proc;
    }

    void OnDestroy()
    {
        if (_particleMaterial != null)
            Destroy(_particleMaterial);
        if (_particleTexture != null)
            Destroy(_particleTexture);
    }

    void HandleSkill3Proc()
    {
        RefreshState();

        // 每次取得一層技能3儲存時播放一次爆發火花。
        if (_skills.Skill3ProcReady && _burst != null)
            _burst.Emit(22);
    }

    void RefreshState()
    {
        if (_skills == null || _aura == null) return;

        bool isReady = _skills.Skill3ProcReady;
        if (isReady == _wasReady) return;
        _wasReady = isReady;

        if (isReady)
        {
            _aura.Play(true);
        }
        else
        {
            _aura.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (_burst != null)
                _burst.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    ParticleSystem CreateAuraParticles()
    {
        var go = new GameObject("Skill3ReadyAura");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = new Vector3(0f, 0.85f, 0f);
        go.transform.localScale = Vector3.one * VFX_SCALE;

        var particles = go.AddComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = particles.main;
        main.playOnAwake = false;
        main.loop = true;
        main.duration = 1.2f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.3f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.18f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.07f, 0.14f);
        main.startColor = new ParticleSystem.MinMaxGradient(ReadyGold, ReadyYellow);
        main.maxParticles = 36;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;

        var emission = particles.emission;
        emission.rateOverTime = 14f;

        var shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.82f;
        shape.radiusThickness = 0.18f;
        shape.rotation = new Vector3(90f, 0f, 0f);

        var velocity = particles.velocityOverLifetime;
        velocity.enabled = true;
        velocity.y = new ParticleSystem.MinMaxCurve(0.32f);
        velocity.orbitalY = new ParticleSystem.MinMaxCurve(0.95f);

        var noise = particles.noise;
        noise.enabled = true;
        noise.strength = 0.12f;
        noise.frequency = 0.65f;
        noise.scrollSpeed = 0.25f;

        var colorOverLifetime = particles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        colorOverLifetime.color = CreateFadeGradient();

        ConfigureRenderer(particles, _particleMaterial);
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        return particles;
    }

    ParticleSystem CreateBurstParticles()
    {
        var go = new GameObject("Skill3ReadyBurst");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = new Vector3(0f, 0.9f, 0f);
        go.transform.localScale = Vector3.one * VFX_SCALE;

        var particles = go.AddComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = particles.main;
        main.playOnAwake = false;
        main.loop = false;
        main.duration = 0.6f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.7f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.1f, 2.0f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.17f);
        main.startColor = new ParticleSystem.MinMaxGradient(ReadyYellow, ReadyGold);
        main.maxParticles = 32;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;

        var emission = particles.emission;
        emission.enabled = false;

        var shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.5f;
        shape.radiusThickness = 0.25f;

        var colorOverLifetime = particles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        colorOverLifetime.color = CreateFadeGradient();

        ConfigureRenderer(particles, _particleMaterial);
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        return particles;
    }

    static ParticleSystem.MinMaxGradient CreateFadeGradient()
    {
        var gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(ReadyYellow, 0f),
                new GradientColorKey(ReadyGold, 1f)
            },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(1f, 0.15f),
                new GradientAlphaKey(0f, 1f)
            });
        return new ParticleSystem.MinMaxGradient(gradient);
    }

    static Texture2D CreateSoftParticleTexture()
    {
        const int size = 32;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false, true)
        {
            name = "Skill3ReadyVfx_SoftCircle (Runtime)",
            hideFlags = HideFlags.DontSave,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        var pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float nx = (x + 0.5f) / size * 2f - 1f;
                float ny = (y + 0.5f) / size * 2f - 1f;
                float distance = Mathf.Sqrt(nx * nx + ny * ny);
                float alpha = Mathf.Clamp01(1f - distance);
                alpha *= alpha;
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply(false, true);
        return texture;
    }

    static Material CreateParticleMaterial(Texture particleTexture)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        if (shader == null) return null;

        var material = new Material(shader)
        {
            name = "Skill3ReadyVfx_Material (Runtime)",
            hideFlags = HideFlags.DontSave,
            renderQueue = (int)RenderQueue.Transparent
        };

        // URP Particle Unlit 的透明加亮設定；不存在的屬性會被安全忽略。
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", Color.white);
        if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", particleTexture);
        if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", particleTexture);
        if (material.HasProperty("_Surface")) material.SetFloat("_Surface", 1f);
        if (material.HasProperty("_Blend")) material.SetFloat("_Blend", 2f);
        if (material.HasProperty("_SrcBlend")) material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        if (material.HasProperty("_DstBlend")) material.SetFloat("_DstBlend", (float)BlendMode.One);
        if (material.HasProperty("_ZWrite")) material.SetFloat("_ZWrite", 0f);
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        return material;
    }

    static void ConfigureRenderer(ParticleSystem particles, Material material)
    {
        var renderer = particles.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.alignment = ParticleSystemRenderSpace.View;
        renderer.sortMode = ParticleSystemSortMode.Distance;
        renderer.maxParticleSize = 0.2f;
        renderer.sortingFudge = 1f;
        if (material != null) renderer.sharedMaterial = material;
    }
}
