//This script adds a new asset option in the project
using UnityEngine;

[CreateAssetMenu(menuName = "STING/Scene Destination")]
public class SceneDestination : ScriptableObject
{
    [Header("Target Scene")]
    public string sceneName;

    [Header("Spawn Transform (world)")]
    public Vector3 position;
    public Vector3 eulerRotation;
}