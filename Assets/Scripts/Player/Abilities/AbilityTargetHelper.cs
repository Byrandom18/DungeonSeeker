using System.Collections.Generic;
using UnityEngine;

public static class AbilityTargetHelper
{
    public static Vector3 ToGameplayPlane(Vector3 worldPoint, Transform reference)
    {
        worldPoint.z = reference != null ? reference.position.z : 0f;
        return worldPoint;
    }

    public static Vector3 ScreenToGameplayPlane(Camera camera, Vector2 screenPosition, Transform reference)
    {
        if (camera == null)
            return ToGameplayPlane(screenPosition, reference);

        float depth = reference != null
            ? camera.WorldToScreenPoint(reference.position).z
            : Mathf.Abs(camera.transform.position.z);

        Vector3 world = camera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, depth));
        return ToGameplayPlane(world, reference);
    }

    public static ICharacterEntity FindAllyAtPosition(Vector3 aim, float maxRange, ICharacterEntity caster)
    {
        if (PartyManager.Instance == null)
            return caster;

        ICharacterEntity best = null;
        float bestDist = float.MaxValue;

        foreach (ICharacterEntity member in PartyManager.Instance.Members)
        {
            if (member == null || !member.IsAlive) continue;

            float dist = Vector2.Distance(aim, member.Transform.position);
            if (dist > maxRange) continue;

            if (dist < bestDist)
            {
                bestDist = dist;
                best = member;
            }
        }

        return best ?? caster;
    }

    public static void SpawnEffectAt(AbilitySO data, Vector3 position, Transform planeReference = null)
    {
        if (data.EffectPrefab == null) return;
        Object.Instantiate(data.EffectPrefab, ToGameplayPlane(position, planeReference), Quaternion.identity);
    }
}
