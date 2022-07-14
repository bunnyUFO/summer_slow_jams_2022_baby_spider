using LateExe;
using System.Collections.Generic;
using UnityEngine;

public class AntLeg : MonoBehaviour
{
    [Header ("Step Configurations")]
    [SerializeField] float stepDistance = 0.2f;
    [SerializeField] float stepHeight = 0.2f;
    [SerializeField] public float stepDuration = 0.2f;
    [SerializeField] List<AntLeg> LegConstraints = null;
    
    [Header ("Shared Raycast Configurations")]
    [SerializeField] Transform body;
    [SerializeField] LayerMask raycastLayer = default;

    [Header ("forward Raycast Configurations")]
    [SerializeField] float forwardRayCastDistance = 2f;
    [SerializeField] float forwardRayCastRadius = 2f;
    [SerializeField] float forwardRayCastOffsetZ = 1f;
    [SerializeField] float forwardRayCastOffsetY = 1f;
    [SerializeField] bool debugBodyForward = false;

    [Header ("Leg Down Raycast Configurations")]
    [SerializeField] float downRayCastDistance = 1f;
    [SerializeField] float downRayCastRadius = 1f;
    [SerializeField] float downRayCastOffsetY = 1f;
    [SerializeField] bool debugLegDown = false;
    
    [Header ("Body Down Raycast Configurations")]
    [SerializeField] float centerRayCastDistance = 2f;
    [SerializeField] float  centerRayCastOffsetY= 0.1f;
    [SerializeField] float  centerRayCastOffsetX= 0.3f;
    [SerializeField] float centerRayCastRadius = 2f;
    [SerializeField] bool debuglegSideways = false;
    [SerializeField] bool debugBodySideways = false;

    public Vector3 stepNormal;
    private Transform _rayCastSource;
    private Vector3 _oldPosition, _currentPosition, _targetPosition, _raycastPosition,_bodyPosition;
    private float _lerp, _lerpTime;
    private bool _raycastHt;
    private Executer _exe;
    
    private void Awake()
    {
        _rayCastSource = transform.parent.transform.Find("raycast_source").transform;
        _lerp = _lerpTime = 0f;
        _oldPosition = _currentPosition = _targetPosition = transform.position;
        _raycastPosition = _rayCastSource.position;
        _exe = new Executer(this);
    }
    
    public void UpdatePosition(float deltaTime)
    {
        UpdatePositions();
        TakeStepIfNeeded(deltaTime);
        DetectSurfaces();
    }

    private void UpdatePositions()
    {
        _raycastPosition = _rayCastSource.position;
        _bodyPosition = body.position;
        transform.position = _currentPosition;
    }

    private void TakeStepIfNeeded(float deltaTime)
    {
        if (Vector3.Distance(_currentPosition, _targetPosition) > stepDistance && !IsMoving() && !OtherLegsMoving())
        {
            _lerpTime = 0f;
        }

        _lerp = _lerpTime / stepDuration;

        if (_lerp < 1)
        {
            Vector3 tempPosition = Vector3.Lerp(_oldPosition, _targetPosition, _lerp);
            tempPosition += transform.up * Mathf.Sin(_lerp * Mathf.PI) * stepHeight;
            _currentPosition = tempPosition;
            _lerpTime += deltaTime;
        }
        else
        {
            _oldPosition = _currentPosition;
        }
    }

    private bool DetectSurfaces(float extraRange = 0f)
    {
        Vector3 bodyForwardBodySource = _bodyPosition + transform.up*forwardRayCastOffsetY + transform.forward*(_rayCastSource.localPosition.z - forwardRayCastOffsetZ);
        Vector3 legDownRaySource = _raycastPosition + transform.up * downRayCastOffsetY;
        Vector3 sidewaysCastDirection = (_rayCastSource.right*(-_rayCastSource.localPosition.x)).normalized;
        Vector3 legSidewaysRaySource = _raycastPosition - transform.up*centerRayCastOffsetY + -sidewaysCastDirection*centerRayCastOffsetX;
        Vector3 bodySidewaysRaySource = body.position - transform.up*centerRayCastOffsetY +  (_rayCastSource.right*(_rayCastSource.localPosition.x)).normalized*(centerRayCastDistance/2);
        _raycastHt = false;
        
        /*
         * Ray cast and set target move position
         * Ray cast forward from body first
         * down from legs if no hit
         * sideways from legs if no hit
         * raycast sideways from body
        */
        if  (Physics.SphereCast(bodyForwardBodySource , forwardRayCastRadius, transform.forward.normalized, out RaycastHit forwardHit, forwardRayCastDistance, raycastLayer.value))
        {
            _targetPosition = forwardHit.point;
            stepNormal = forwardHit.normal;
            _raycastHt = true;
        }
        else if (Physics.SphereCast(legDownRaySource , downRayCastRadius, -transform.up.normalized, out RaycastHit downHit, downRayCastDistance + extraRange, raycastLayer.value))
        {
            _targetPosition = downHit.point;
            stepNormal = downHit.normal;
            _raycastHt = true;
        }
        else if  (Physics.SphereCast(legSidewaysRaySource , centerRayCastRadius, sidewaysCastDirection.normalized, out RaycastHit horizontal, centerRayCastDistance, raycastLayer.value))
        {
            _targetPosition = horizontal.point;
            stepNormal = horizontal.normal;
            _raycastHt = true;
        }
        else if  (Physics.SphereCast(bodySidewaysRaySource , centerRayCastRadius, sidewaysCastDirection.normalized, out RaycastHit centerHit, centerRayCastDistance, raycastLayer.value))
        {
            _targetPosition = centerHit.point;
            stepNormal = centerHit.normal;
            _raycastHt = true;
        }

        
        return _raycastHt;
    }
    
    public bool OtherLegsMoving()
    {
        return LegConstraints.TrueForAll(leg => leg.IsMoving());
    }
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(_targetPosition, 0.02f);
        Gizmos.DrawLine(_oldPosition, _targetPosition);
        
        Transform castTransform = transform.parent.transform.Find("raycast_source").transform;
        Vector3 castSource = castTransform.position;
        
        Vector3 castTransformRight = transform.parent.transform.Find("raycast_source").transform.right;

        if (debugLegDown)
        {
            Vector3 source = castSource + transform.up * downRayCastOffsetY;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(source, downRayCastRadius);
            Gizmos.DrawLine(source, source + -transform.up.normalized*downRayCastDistance);
            Gizmos.DrawWireSphere(source + -transform.up.normalized*downRayCastDistance, downRayCastRadius);
        }

        if (debugBodyForward)
        {
            Gizmos.color = Color.red;
            Vector3 source = body.position + transform.up*forwardRayCastOffsetY + transform.forward*(castTransform.localPosition.z - forwardRayCastOffsetZ);
            Gizmos.DrawWireSphere(source, forwardRayCastRadius);
            Gizmos.DrawLine(source, source + transform.forward.normalized*forwardRayCastDistance);
            Gizmos.DrawWireSphere(source + transform.forward.normalized*forwardRayCastDistance, forwardRayCastRadius);
        }
        if (debuglegSideways)
        {
            Vector3 bodySidewaysRaySourceSidewaysRaySource = (castTransformRight*(-castTransform.localPosition.x)).normalized;
            Vector3 legSidewaysRaySource = castSource - transform.up*centerRayCastOffsetY + -bodySidewaysRaySourceSidewaysRaySource*centerRayCastOffsetX;
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(legSidewaysRaySource, centerRayCastRadius);
            Gizmos.DrawLine(legSidewaysRaySource, legSidewaysRaySource + bodySidewaysRaySourceSidewaysRaySource.normalized*centerRayCastDistance);
            Gizmos.DrawWireSphere(legSidewaysRaySource + bodySidewaysRaySourceSidewaysRaySource.normalized*centerRayCastDistance, centerRayCastRadius);
        }
        
        if (debugBodySideways)
        {
            Vector3 source = body.position - transform.up*centerRayCastOffsetY +  (castTransformRight*(castTransform.localPosition.x)).normalized*(centerRayCastDistance/2);
            Vector3 bodySidewaysRaySource = (castTransformRight*(-castTransform.localPosition.x)).normalized;
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(source, centerRayCastRadius);
            Gizmos.DrawLine(source, source + bodySidewaysRaySource.normalized*centerRayCastDistance);
            Gizmos.DrawWireSphere(source + bodySidewaysRaySource.normalized*centerRayCastDistance, centerRayCastRadius);
        }
    }
    public bool IsMoving()
    {
        return _lerp < 1;
    }
    public void Stagger(float delay = 0f)
    {
        _exe.DelayExecute(delay , x=> _lerpTime = 0);
    }

    public bool Grounded()
    {
        return _raycastHt;
    }

    public Vector3 GetOldPosition()
    {
        return _oldPosition;
    }
    
    public Vector3 GetTargetPosition()
    {
        return _targetPosition;
    }
}