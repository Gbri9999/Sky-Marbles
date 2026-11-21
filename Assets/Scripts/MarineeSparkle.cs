using UnityEngine;

public class MarineeSparkle : MonoBehaviour
{
    public static void Play(Vector3 position)
    {
        GameObject effect = new GameObject("Marinee blue sparkle");
        effect.transform.position = position;
        ParticleSystem particles = effect.AddComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ParticleSystem.MainModule main = particles.main;
        main.duration = 0.25f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.5f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.2f, 3f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.07f, 0.18f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.02f, 0.24f, 0.75f), new Color(0.2f, 0.9f, 1f));
        main.gravityModifier = 0f;
        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 18) });
        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.3f;
        ParticleSystem.ColorOverLifetimeModule fade = particles.colorOverLifetime;
        fade.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
        fade.color = gradient;
        particles.Play();
        Destroy(effect, 1f);
    }
}
