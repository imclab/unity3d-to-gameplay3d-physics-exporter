using UnityEngine;

/// <summary>
/// Attach this to a GameObject that has a RigidBody to define the parameters in
/// PhysicsRigidBody::Parameters that aren't exported from Unity3D.
/// </summary>
public class Gameplay3DRigidBodyParams : MonoBehaviour
{
    public float Friction = 0.5f;
    public float Restitution = 0.0f;
    public float LinearDamping = 0.0f;
    public float AngularDamping = 0.0f;
    public Vector3 AnisotropicFriction = Vector3.one;
}
