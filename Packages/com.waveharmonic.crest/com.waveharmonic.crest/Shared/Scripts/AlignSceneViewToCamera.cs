// Crest Water System
// Copyright © 2024 Wave Harmonic. All rights reserved.

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using WaveHarmonic.Crest.Internal.Compatibility;

namespace WaveHarmonic.Crest.Examples
{
#if !CREST_DEBUG
    [AddComponentMenu("")]
#endif
    [ExecuteAlways]
    [RequireComponent(typeof(Camera))]
    sealed class AlignSceneViewToCamera : MonoBehaviour
    {
        [SerializeField, HideInInspector]
#pragma warning disable 414
        int _Version = 0;
#pragma warning restore 414

#if UNITY_EDITOR
        static ulong s_Scene;
        static bool s_SceneChanged;

        [InitializeOnLoadMethod]
        static void OnLoad()
        {
            EditorSceneManager.sceneClosed -= OnSceneClosed;
            EditorSceneManager.sceneClosed += OnSceneClosed;
            s_Scene = SceneManager.GetActiveScene().GetRawSceneHandle();
        }

        static void OnSceneClosed(Scene a)
        {
            // TODO: Report to Unity
            // Does not work if only game view is open. Handles will never update.
            if (s_Scene == a.GetRawSceneHandle()) return;
            s_SceneChanged = true;
            s_Scene = a.GetRawSceneHandle();
        }

        void OnEnable()
        {
            EditorApplication.update -= EditorUpdate;
            EditorApplication.update += EditorUpdate;
        }

        void OnDisable()
        {
            EditorApplication.update -= EditorUpdate;
        }

        void EditorUpdate()
        {
            var water = WaterRenderer.Instance;
            if (s_SceneChanged && SceneView.lastActiveSceneView != null && water != null && water.IsSceneViewActive)
            {
                TeleportSceneCamera(transform);
                s_SceneChanged = false;
            }
        }

        public static void TeleportSceneCamera(Transform transform)
        {
            var view = SceneView.lastActiveSceneView;
            if (view == null) return;
            view.pivot = transform.position + transform.forward * view.cameraDistance;
            view.rotation = Quaternion.LookRotation(transform.forward);
        }
#endif
    }
}
