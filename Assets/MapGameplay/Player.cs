using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Serialization;

public class Player : MonoBehaviour
{
    //static part///////////////////////////////////////////////////////////////////////////////////////////////////////
    
    private static readonly int JumpedAnimatorPropertyID = Animator.StringToHash("Jumped");
    private static readonly int GroundedAnimatorPropertyID = Animator.StringToHash("Grounded");
    private static readonly int RunningAnimatorPropertyID = Animator.StringToHash("Running");
    private static readonly int DiedAnimatorPropertyID = Animator.StringToHash("Died");
    
    //fields////////////////////////////////////////////////////////////////////////////////////////////////////////////
    [Header("Jump")]
    [SerializeField] private float jumpKick = 15f;
    [SerializeField] private float suppressFactor = 0.5f;
    [SerializeField] private float coyoteTime = 0.05f;
    [SerializeField] private float jumpBufferTime = 0.1f;
    
    [Header("Horizontal Movement")]
    [SerializeField] private float maxSpeed = 8f;
    [SerializeField] private float acceleration = 50f;
    [SerializeField] private float deceleration = 100f;
    
    [Header("Gravity")]
    [SerializeField] private float maxFallSpeed = 40f;
    [SerializeField] private float gravity = 50f;
    
    [Header("Touches")]
    [SerializeField] private LayerMask collisionMask;
    [SerializeField] private float collisionCheckDistance = 0.2f;
    [SerializeField] private float collisionGap = 0.01f;
    
    [Header("Cling")]
    [SerializeField] private Transform leftClingWithAnchor;
    [SerializeField] private Transform rightClingWithAnchor;
    [SerializeField] private LayerMask clingMask;
    
    [Header("Effects")] 
    [SerializeField] private float landingEffectHeightThreshold;
    [SerializeField] private GameObject landingDustPrefab;
    [SerializeField] private Transform landingDustSpawnPoint;
    
    [Header("Dependencies")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private CapsuleCollider2D capsule;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Animator animator;
    [SerializeField] private CustomSoundEmitter soundEmitter;
    [SerializeField] private PermutableSoundPlayer soundPlayer;
    
    private bool _grounded;
    private float _timeUngrounded = float.NegativeInfinity;
    private float _timeTriedJumping = float.NegativeInfinity;
    private float _maxYDuringFall = 0;
    
    private Corpse _clungCorpse = null;
    private Vector2 _clingOffset = Vector2.zero;
    
    //initialisation////////////////////////////////////////////////////////////////////////////////////////////////////
    private void Awake()
    {
        Assert.IsNotNull(rb);
        Assert.IsNotNull(capsule);
        Assert.IsNotNull(spriteRenderer);
        Assert.IsNotNull(animator);
        Assert.IsNotNull(soundEmitter);
        Assert.IsNotNull(soundPlayer);
        
        Assert.IsNotNull(leftClingWithAnchor);
        Assert.IsNotNull(rightClingWithAnchor);
        
        Assert.IsNotNull(landingDustPrefab);
        Assert.IsNotNull(landingDustSpawnPoint);
        
        rb.simulated = true;
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.useFullKinematicContacts = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.sleepMode = RigidbodySleepMode2D.NeverSleep;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.freezeRotation = true;
        rb.gravityScale = 0;
        rb.linearDamping = 0;
        rb.angularDamping = 0;
        rb.linearVelocity = Vector2.zero;
        rb.inertia = 0;
        rb.useAutoMass = false;
        rb.mass = 0;
    }


    //public interface//////////////////////////////////////////////////////////////////////////////////////////////////

    public Rigidbody2D Body => rb;
    public bool Flip => spriteRenderer.flipX;
    
    public float HorizontalOrderDirection { get; set; }
    public bool OrderedToCling { get; set; }
    
    public void PrepareForDeath()
    {
        gameObject.layer = 10;
        animator.SetBool(DiedAnimatorPropertyID, true);
    }
    
    public void MakeRegularJump()
    {
        _timeTriedJumping = Time.time;
        if ((_grounded || _timeUngrounded + coyoteTime > Time.time) && !_clungCorpse)
            Jump(jumpKick);
    }

    public void SuppressJump()
    {
        if (rb.linearVelocity.y > 0)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * suppressFactor);
    }

    //game events///////////////////////////////////////////////////////////////////////////////////////////////////////
    private void FixedUpdate()
    {
        //update max y since ungrounded
        if (!_grounded && _maxYDuringFall < rb.transform.position.y)
            _maxYDuringFall = rb.transform.position.y;
        
        
        var isGroundHit = CastBodyTo(Vector2.down, collisionCheckDistance, collisionMask, out var groundHitData);
        var isGroundDirt = isGroundHit && groundHitData.collider.CompareTag("Dirt");
        
        if (isGroundHit && !_grounded)
        {
            _grounded = true;
            _timeUngrounded = float.NegativeInfinity;
            if (_timeTriedJumping + jumpBufferTime > Time.time && !_clungCorpse)
                Jump(jumpKick);
            
            soundPlayer.SelectClip(isGroundDirt ? "WalkDirt" : "WalkSolid");
            if (_maxYDuringFall - rb.transform.position.y > landingEffectHeightThreshold)
            {
                Instantiate(landingDustPrefab, landingDustSpawnPoint.position, Quaternion.identity);
                soundEmitter.EmitSound(isGroundDirt ? "LandingDirt" : "LandingSolid");
            }
            _maxYDuringFall = 0;
        }
        else if (!isGroundHit && _grounded)
        {
            _grounded = false;
            _timeUngrounded = Time.time;
            _maxYDuringFall = rb.transform.position.y;
            animator.SetBool(JumpedAnimatorPropertyID, false);
            soundPlayer.UnselectClip();
        }

        
        
        var hor = HorizontalOrderDirection;
        var moveOrdered = !Mathf.Approximately(hor, 0);
        var facingRight = moveOrdered ? hor > 0 : spriteRenderer.flipX;

        
        if (OrderedToCling && !_clungCorpse
            && CastBodyTo(facingRight ? Vector2.right : Vector2.left, collisionCheckDistance, clingMask, out var corpseHit))
        {
            var corpse = corpseHit.collider.GetComponentInParent<Corpse>();
            Assert.IsNotNull(corpse);
            var offset = facingRight
                ? corpse.LeftClingLocal - rightClingWithAnchor.localPosition.To2()
                : corpse.RightClingLocal - leftClingWithAnchor.localPosition.To2();
            if (!CastBodyAt(corpse.Position + offset, collisionMask, out var _))
            {
                _clungCorpse = corpse;
                _clingOffset = offset;
            }
        }
        else if (!OrderedToCling && _clungCorpse)
            _clungCorpse = null;


        if (!_clungCorpse)
        {
            if (moveOrdered && hor * rb.linearVelocityX < 0) //different directions -> different signs -> mult is negative
                rb.linearVelocityX = 0;

            rb.linearVelocityX = moveOrdered
                ? Mathf.MoveTowards(rb.linearVelocityX, hor * maxSpeed, Time.fixedDeltaTime * acceleration)
                : Mathf.MoveTowards(rb.linearVelocityX, 0, Time.fixedDeltaTime * deceleration);
        
            rb.linearVelocityY = _grounded 
                ? rb.linearVelocityY.ClampBottom(0)
                : Mathf.MoveTowards(rb.linearVelocityY, -maxFallSpeed, Time.fixedDeltaTime * gravity);
        }
        else
        {
            rb.MovePosition(_clungCorpse.Position + _clingOffset);
        }
        
        
        animator.SetBool(RunningAnimatorPropertyID, moveOrdered);
        animator.SetBool(GroundedAnimatorPropertyID, _grounded);

        spriteRenderer.flipX = moveOrdered 
            ? hor < 0 
            : spriteRenderer.flipX;
        
        //TODO: stop calling it every frame
        if (moveOrdered)
            soundPlayer.PlayAll();
        else
            soundPlayer.Stop();
        
        
        
        var predictedDeltaY = rb.linearVelocityY * Time.fixedDeltaTime;
        var vertDir = predictedDeltaY > 0 ? Vector2.up : Vector2.down;
        if (!predictedDeltaY.IsApproximately(0) 
            && CastBodyTo(vertDir, predictedDeltaY.Abs(), collisionMask, out var vertCCDHit))
        {
            rb.position += vertDir * (vertCCDHit.distance - collisionGap);
            rb.linearVelocityY = 0;
        }
        var predictedDeltaX = rb.linearVelocityX * Time.fixedDeltaTime;
        var horDir = predictedDeltaX > 0 ? Vector2.right : Vector2.left;
        if (!predictedDeltaX.IsApproximately(0) 
            && CastBodyTo(horDir, predictedDeltaX.Abs(), collisionMask, out var horCCDHit))
        {
            rb.position += horDir * (horCCDHit.distance - collisionGap);
            rb.linearVelocityX = 0;
        }
        
        // experimental
        var predictedDelta = rb.linearVelocity * Time.fixedDeltaTime;
        var distance = predictedDelta.magnitude;
        var direction = predictedDelta.normalized;
        if (!distance.IsApproximately(0)
            && CastBodyTo(direction, distance, collisionMask, out var directionCCDHit))
        {
            rb.position += direction * (directionCCDHit.distance - collisionGap);
            var normal = directionCCDHit.normal;
            rb.linearVelocity -= Vector2.Dot(rb.linearVelocity, normal) * normal;
            rb.linearVelocity *= 0.5f;
        }
    }
    
    
    
    // private void OnCollisionStay2D(Collision2D collision)
    // {
    //     var totalPenetrationVector = Vector2.zero;
    //     foreach (var contact in collision.contacts) 
    //         totalPenetrationVector += contact.normal * Mathf.Abs(contact.separation);
    //     totalPenetrationVector /= collision.contactCount;
    //
    //     Debug.Log(totalPenetrationVector);
    //     rb.linearVelocity += totalPenetrationVector * penetrationResolutionSpeed;
    //     
    //     // old
    //     // var resolutionMovement = totalPenetrationVector * penetrationResolutionSpeed * Time.fixedDeltaTime;
    //     // rb.position += resolutionMovement;
    //
    //     // Optional: Implement a maximum resolution distance per frame
    //     // resolutionMovement = Vector2.ClampMagnitude(resolutionMovement, maxResolutionDistancePerFrame);
    // }
    
    
    //private logic/////////////////////////////////////////////////////////////////////////////////////////////////////
    private void Jump(float kick)
    {
        rb.linearVelocityY = kick;
        animator.SetBool(JumpedAnimatorPropertyID, true);
        soundEmitter.EmitSound("Jump");
    }

    private bool CastBodyTo(Vector2 direction, float distance, LayerMask layer, out RaycastHit2D hit)
    {
        var originalLayer = capsule.gameObject.layer;
        //TODO: do something with layers
        capsule.gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");

        hit = Physics2D.CapsuleCast(rb.position + capsule.offset, capsule.size, capsule.direction, 0, direction, distance, layer);

        capsule.gameObject.layer = originalLayer;
        return hit;
    }
    
    private bool CastBodyAt(Vector2 position, LayerMask layer, out RaycastHit2D hit)
    {
        var originalLayer = capsule.gameObject.layer;
        //TODO: do something with layers
        capsule.gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");

        hit = Physics2D.CapsuleCast(position, capsule.size, capsule.direction, 0, Vector2.zero, 0, layer);

        capsule.gameObject.layer = originalLayer;
        return hit;
    }
}