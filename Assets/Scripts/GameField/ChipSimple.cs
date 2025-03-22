using UnityEngine;
using System.Collections;


public class ChipSimple : Chip
{
    protected override IEnumerator AnimateDeath()
    {
        if (this is null || gameObject is null)
        {
            Debug.LogWarning("Trying to animate dead chip. Step 1.");
            yield break;
        }

        Vector3 startScale = transform.localScale;
        float elapsedTime = 0;

        while (elapsedTime < deathDuration)
        {
            if (this is null || gameObject is null)
            {
                Debug.LogWarning("Trying to animate dead chip. Step 2.");
                yield break;
            }

            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, elapsedTime / deathDuration);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        NotifyDeathCompleted();
    }
}
