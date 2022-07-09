using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProceduralAnimationScript : MonoBehaviour
{
    [SerializeField] LayerMask raycastLayer = default;
    [SerializeField] float offsetZ = 0;
    [SerializeField] float stepDistance = 0.2f;
    [SerializeField] float stepHeight = 0.2f;
    [SerializeField] float stepDuration = 0.2f;
    [SerializeField] ProceduralAnimationScript oppositeLeg = null;
    public Vector3 stepNormal;
    private Transform rayCastSource;
    private Vector3 _oldPosition, _currentPosition, _targetPosition;
    private float lerp, lerpTime;

    //Set initial IK target position to ground at z offset from source
    private void Awake()
    {
        lerp = 1;
        lerpTime = stepDuration;
        rayCastSource = transform.parent.transform.Find("raycast_source").transform;
        Vector3 initalRayCastSource = transform.position + transform.up.normalized*0.08f;
        Ray ray = new Ray(initalRayCastSource, -transform.up.normalized);

        if (Physics.Raycast(ray, out RaycastHit info, 1, raycastLayer.value))
        {
            _oldPosition = _currentPosition =_targetPosition = info.point + Vector3.forward*offsetZ;
        }
    }
    
    public void UpdatePosition(float deltaTime)
    {
        transform.position = _currentPosition;
        
        if (Physics.SphereCast(rayCastSource.position, stepDistance, -transform.up.normalized, out RaycastHit hit, 1f, raycastLayer.value)) {
            _targetPosition = hit.point;
            stepNormal = hit.normal;

            if (Vector3.Distance(_currentPosition, _targetPosition) > stepDistance && lerp >= 1 && oppositeLeg.IsGrounded())
            {
                lerpTime = 0f;
            }
        }

        lerp = lerpTime/stepDuration;
        
        if (lerp < 1)
        {
            Vector3 tempPosition = Vector3.Lerp(_oldPosition, _targetPosition, lerp);
            tempPosition.y += Mathf.Sin(lerp * Mathf.PI) * stepHeight;
            
            _currentPosition = tempPosition;
            lerpTime += deltaTime;
        }
        else
        {
            _oldPosition = _currentPosition;
        }
    }

    public Vector3 GetOldPosition()
    {
        return _oldPosition;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(_targetPosition, 0.02f);
        Gizmos.DrawLine(_oldPosition, _targetPosition);
    }
    public bool IsGrounded()
    {
        return lerp >= 1;
    }
}