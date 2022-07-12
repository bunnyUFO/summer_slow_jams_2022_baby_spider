using System.Collections.Generic;
using UnityEngine;

public class Spider : MonoBehaviour
{
    [Header("Body Positioning")] [LabelOverride("Body Y Offset")] [SerializeField]
    float offsetY = 0.2f;
    [LabelOverride("Rotation Speed Curve")] [SerializeField]
    AnimationCurve sensitivityCurve;
    [SerializeField] List<ProceduralAnimationScript> legs;
    [SerializeField] bool debug= false;
    private Rigidbody _rigidbody;
    private bool _stagger = false;

    private void Awake()
    {
        _rigidbody = transform.GetComponent<Rigidbody>();
    }

    void Update()
    {
        if (transform.hasChanged)
        {
            foreach (ProceduralAnimationScript leg in legs)
            {
                leg.UpdatePosition(Time.deltaTime);
            }
        }
        StaggerLegs();
        CalculateOrientation();
    }

    private void CalculateOrientation()
    {
        Vector3 up = Vector3.zero;
        float avgSurfaceDist = 0;

        Vector3 point, a, b, c;

        for (int i = 0; i < legs.Count; i++)
        {
            // Calculate adjacent leg distance from body
            ProceduralAnimationScript legPair = i == 0 ? legs[legs.Count - 1] : legs[i - 1];
            point = legs[i].GetOldPosition();
            avgSurfaceDist += transform.InverseTransformPoint(point).y;
            a = (transform.position - point).normalized;
            b = (legPair.GetOldPosition() - point).normalized;
            
            // Calculate product of adjacent leg distance from body
            c = Vector3.Cross(a, b);
            
            // Use leg surface normal vectors and leg distance cross products to calculate body up vector
            // up += (legs[i].stepNormal == Vector3.zero ? transform.forward : legs[i].stepNormal);
            up += c * sensitivityCurve.Evaluate(c.magnitude) +
                  (legs[i].stepNormal == Vector3.zero ? transform.forward : legs[i].stepNormal);
            
            if (debug)
            {
                Debug.DrawRay(point, c, Color.yellow, 0);
                Debug.DrawRay(point, legs[i].stepNormal, Color.magenta, 0);
            }
        }

        // Scale up vector and surface vertical distance
        up /= legs.Count;
        avgSurfaceDist /= legs.Count;
        if (debug)
        {
            Debug.DrawRay(transform.position, up, Color.green, 0);
        }
        
        if (true)
        {
            // _rigidbody.AddForce(Vector3.zero);
            transform.rotation = Quaternion.Slerp(transform.rotation,
                Quaternion.LookRotation(Vector3.ProjectOnPlane(transform.forward, up), up), 22.5f * Time.deltaTime);
            transform.Translate(0, -(-avgSurfaceDist + -offsetY) * 0.5f, 0, Space.Self);
        }
        else
        {
            //rotate body to nearest surface??
            // _rigidbody.AddForce(Vector3.down);
        }
    }

    public void StaggerLegs(bool stagger)
    {
        _stagger = stagger;
    }

    public void StaggerLegs()
    {
        if(_stagger && legs.TrueForAll(leg => !leg.IsMoving()))
        {
            _stagger = false;
            for (int i = 0; i < legs.Count; i++)
            {
                float delay = (((i/4) + (i%2)) * legs[i].stepDuration)/2f;
                legs[i].Stagger(delay);
            }
        }
    }
}