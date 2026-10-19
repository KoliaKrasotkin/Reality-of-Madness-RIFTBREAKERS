using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

namespace MultiFPS.Gameplay
{
    /// <summary>
    /// Script responsible for character movement
    /// </summary>
    [RequireComponent(typeof(CharacterInstance))]
    public class CharacterMotor : NetworkBehaviour
    {
        Transform _myTransform;

        CharacterInstance _charInstance;
        CharacterController _controller;

        public float WalkSpeed = 5f;
        public float RunSpeed = 10f;
        public float CrouchSpeed = 10f;
        public float JumpHeight;
        public float Gravity = 10f;
        [SerializeField, Range(0f, 500f)]
        float maxAcceleration = 28f;
        [SerializeField] float brakingAcceleration = 40f;
        [SerializeField, Range(0f, 1f)] float airControl = .3f;
        [SerializeField] float coyoteTime = .1f;
        [SerializeField] float jumpBuffer = .12f;
        float lastGroundedTime = float.NegativeInfinity;
        float jumpRequestedTime = float.NegativeInfinity;
        public Vector3 PlanarVelocity { get; private set; }
        public bool WantsToSprint => _charInstance.ReadActionKeyCode(ActionCodes.Sprint)
            && !_charInstance.IsCrouching && !_charInstance.IsScoping
            && !_charInstance.IsUsingItem && !_charInstance.Block
            && !_charInstance.ReadActionKeyCode(ActionCodes.Trigger1)
            && !_charInstance.ReadActionKeyCode(ActionCodes.Trigger2)
            && _charInstance.Input.Movement.y > .1f;
        public float CameraLerpSpeed = 2f;
        float _speed;

        Vector3 force;


        Vector3 defaultAttackPos;
        Vector3 crouchAttackPos = new (0,0.7f,0);

        Vector3 _velocity;

        #if UNITY_EDITOR
        bool _noclip = false;
        [SerializeField] float noclipSpeed = 10;
        [SerializeField] float noclipRunSpeed = 30;
        #endif

        public Vector3 cameraTargetPosition;

        bool isGrounded;




        #region for falldamage
        float lastYPos;
        float _startFallingPointY;
        bool _falling;
        #endregion

        void Awake()
        {
            _myTransform = transform;
            _charInstance = GetComponent<CharacterInstance>();
            _controller = GetComponent<CharacterController>();
        }

        private void Start()
        {
            defaultAttackPos = _charInstance.Health.centerPosition;
            Stand();
        }

        private void OnDestroy()
        {
            _charInstance = null;
            _controller = null;
        }

        void Update()
        {
            #if UNITY_EDITOR
            if (isOwned && Input.GetKeyDown(KeyCode.F8))
                _noclip = !_noclip;
            #endif

            if (_charInstance.FPPLook.localPosition != cameraTargetPosition)
                _charInstance.FPPLook.localPosition = Vector3.MoveTowards(_charInstance.FPPLook.localPosition, cameraTargetPosition, CameraLerpSpeed * Time.deltaTime);
        }
        private void FixedUpdate()
        {
            if (_charInstance.Health.CurrentHealth <= 0) return;

            if (!_charInstance.IsCrouching)
            {
                if (_charInstance.ReadActionKeyCode(ActionCodes.Crouch) && _charInstance.isGrounded  )
                {
                    Crouch();
                    _charInstance.IsCrouching = true;
                }
            }
            else
            {
                if (!_charInstance.ReadActionKeyCode(ActionCodes.Crouch) && !CheckSphere())
                {
                    Stand();
                    _charInstance.IsCrouching = false;
                }
            }

            if(isOwned || (isServer && _charInstance.BOT))
                MovementTick();

            if (!(isOwned || (isServer && _charInstance.BOT)))
                isGrounded = GroundProbe();
            _charInstance.isGrounded = isGrounded;

            if (!isServer) return;

            //count fall damage
            if (!_charInstance.isGrounded)
            {
                if (!_falling)
                {
                    _falling = true;
                    _startFallingPointY = _myTransform.position.y;
                    lastYPos = _startFallingPointY + 1f;
                }
                else
                {
                    if (_myTransform.position.y >= lastYPos)
                    {
                        _falling = false;
                    }

                    lastYPos = _myTransform.position.y;
                }

                if (_startFallingPointY < _myTransform.position.y)
                    _startFallingPointY = _myTransform.position.y;
            }
            else
            {
                if (_falling)
                {
                    float distance = _startFallingPointY - _myTransform.position.y;
                    if (distance > 5.5f)
                    {
                        _charInstance.Health.Server_ChangeHealthState(Mathf.FloorToInt(
                            distance * distance * 0.5f), 0, 
                            AttackType.falldamage, _charInstance.Health, 0);
                    }
                    _falling = false;
                }
            }
        }

        public void MovementTick() 
        {
            #if UNITY_EDITOR
            if (_noclip && isOwned) 
            {
                _charInstance.PrepareCharacterToLerp();
                Vector3 noclipInput = new (_charInstance.Input.Movement.x, (Input.GetKey(KeyCode.LeftControl)? -1: Input.GetKey(KeyCode.Space)? 1f: 0), _charInstance.Input.Movement.y);
                noclipInput = _charInstance.FPPCameraTarget.rotation * noclipInput;
                float speed = _charInstance.ReadActionKeyCode(ActionCodes.Sprint) ? noclipRunSpeed : noclipSpeed;
                noclipInput = noclipInput * speed;
                _myTransform.position += noclipInput * Time.fixedDeltaTime;
                _charInstance.SetCurrentPositionTargetToLerp(_myTransform.position);
                return;
            }
            #endif


            if (!isOwned && !_charInstance.BOT) return;

            if (_charInstance.Health.CurrentHealth <= 0) return;

            _charInstance.PrepareCharacterToLerp();

            float dt = Time.fixedDeltaTime;
            isGrounded = force.y <= 0f && GroundProbe();
            if (isGrounded) lastGroundedTime = Time.time;
            _charInstance.IsRunning = WantsToSprint && isGrounded;
            _speed = _charInstance.IsCrouching ? CrouchSpeed : (_charInstance.IsRunning ? RunSpeed : WalkSpeed);
            Vector2 input = _charInstance.Block ? Vector2.zero : Vector2.ClampMagnitude(_charInstance.Input.Movement, 1f);
            Vector3 desired = Quaternion.Euler(0, _charInstance.Input.LookY, 0)
                * new Vector3(input.x, 0, input.y) * _speed;
            float acceleration = input.sqrMagnitude < .001f ? brakingAcceleration : maxAcceleration;
            _velocity = Vector3.MoveTowards(_velocity, desired, acceleration * (isGrounded ? 1f : airControl) * dt);
            if (Time.time - jumpRequestedTime <= jumpBuffer
                && Time.time - lastGroundedTime <= coyoteTime && !_charInstance.IsCrouching && !CheckSphere())
            {
                force.y = Mathf.Sqrt(2f * Gravity * JumpHeight);
                jumpRequestedTime = lastGroundedTime = float.NegativeInfinity;
                isGrounded = false;
            }
            else if (isGrounded) force.y = -2f;
            else force.y = Mathf.Max(force.y - Gravity * dt, -55f);
            Vector3 before = _myTransform.position;
            CollisionFlags flags = _controller.Move((_velocity + force) * dt);
            if ((flags & CollisionFlags.Above) != 0 && force.y > 0) force.y = 0;
            isGrounded = force.y <= 0 && ((flags & CollisionFlags.Below) != 0 || GroundProbe());
            Vector3 moved = (_myTransform.position - before) / dt;
            // Read controller motion rather than a Transform delta that can include
            // deferred physics/network corrections; those are not extra footsteps.
            Vector3 actual = Application.isPlaying ? _controller.velocity : moved;
            PlanarVelocity = Vector3.ClampMagnitude(new Vector3(actual.x, 0, actual.z), _velocity.magnitude);
            // Drop accumulated push into walls so reversing direction does not feel sticky.
            if ((flags & CollisionFlags.Sides) != 0) _velocity = PlanarVelocity;

            _charInstance.SetCurrentPositionTargetToLerp(_myTransform.position);
        }

        void Crouch() 
        {
            _controller.center = new Vector3(0, 0.5f, 0);
            _controller.height = 1f;

            _charInstance.Health.centerPosition = crouchAttackPos;

            LerpCamera(1f);
        }
        void Stand() 
        {
            _controller.center = new Vector3(0,1,0);
            _controller.height = 2f;

            _charInstance.Health.centerPosition = defaultAttackPos;

            LerpCamera(_charInstance.CameraHeight);
        }

        void LerpCamera(float cameraHeight) 
        {
            cameraTargetPosition = new Vector3(0, cameraHeight, 0);
        }

        public void Jump()
        {
            jumpRequestedTime = Time.time;
        }

        bool GroundProbe()
        {
            if (!_controller.enabled) return false;
            float radius = Mathf.Max(.05f, _controller.radius * .85f);
            Vector3 bottom = _myTransform.position + _controller.center
                - Vector3.up * (_controller.height * .5f - _controller.radius);
            return Physics.SphereCast(bottom + Vector3.up * .08f, radius, Vector3.down,
                out var hit, _controller.radius - radius + _controller.skinWidth + .2f,
                GameManager.environmentLayer, QueryTriggerInteraction.Ignore)
                && Vector3.Angle(hit.normal, Vector3.up) <= _controller.slopeLimit;
        }

        bool CheckSphere()
        {
            // Check only the extra headroom needed above the crouching capsule.
            float radius = Mathf.Max(.05f, _controller.radius - _controller.skinWidth);
            return Physics.CheckCapsule(_myTransform.position + Vector3.up * (1f + radius),
                _myTransform.position + Vector3.up * (2f - radius), radius,
                GameManager.environmentLayer, QueryTriggerInteraction.Ignore);
        }
    }
}
