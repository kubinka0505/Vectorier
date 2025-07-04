using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

[InitializeOnLoad]
public static class AnimatorAutoPlayInScene
{
    private static List<Animator> animators = new List<Animator>(); // List of animators to track in the scene

    static AnimatorAutoPlayInScene()
    {
        // Ensure UpdateAnimation is called on each editor update
        EditorApplication.update += UpdateAnimation;

        // Automatically find and add all animators in the scene when Unity loads
        EditorApplication.hierarchyChanged += FindAllAnimators;
        FindAllAnimators(); // Initial call to populate the list
    }

    // Find all animators in the scene at startup or hierarchy change
    private static void FindAllAnimators()
    {
        animators.Clear();

        // Find all active animators in the scene
        Animator[] sceneAnimators = Object.FindObjectsOfType<Animator>();
        foreach (Animator animator in sceneAnimators)
        {
            if (IsAnimatorActive(animator))
            {
                animators.Add(animator);
            }
        }
    }

    // Check if the animator is active and its GameObject is also active
    private static bool IsAnimatorActive(Animator animator)
    {
        if (animator == null || !animator.enabled || !animator.gameObject.activeInHierarchy)
        {
            return false;
        }

        // Check if the GameObject is hidden in the hierarchy
        if ((animator.gameObject.hideFlags & HideFlags.HideInHierarchy) != 0)
        {
            return false;
        }

        return true;
    }

    // Update the animation every frame
    private static void UpdateAnimation()
    {
        // Check each animator and update if necessary
        for (int i = animators.Count - 1; i >= 0; i--)
        {
            var animator = animators[i];

            // If the animator is disabled, remove it from the list
            if (!IsAnimatorActive(animator))
            {
                animators.RemoveAt(i);
            }
            // If the animator was previously disabled and is now enabled, add it back
            else if (animator.enabled && !animators.Contains(animator))
            {
                animators.Add(animator);
                // Explicitly play the animation by updating it once immediately
                animator.Update(0f); // Start playing the animation right away
            }
        }

        // Update animations for all active animators
        foreach (var animator in animators)
        {
            if (animator != null && animator.runtimeAnimatorController != null)
            {
                animator.Update(Time.deltaTime); // Update each animator normally, using base speed
            }
        }

        // Refresh the Scene view if there are any animators
        if (animators.Count > 0)
        {
            SceneView.RepaintAll();
        }
    }
}
