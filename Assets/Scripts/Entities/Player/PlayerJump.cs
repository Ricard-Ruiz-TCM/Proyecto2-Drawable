using System;
using UnityEngine;

public class PlayerJump : PlayerState, IHaveStates {

    // Observer para saber cuando salta
    public static event Action OnJump;

    [SerializeField]
    private bool _isJumping;
    public bool IsJumping() { return _isJumping; }

    // Jump Attributes
    [SerializeField]
    private float _jumpStr;
    
    // Jump Controls
    private bool _wallFree;
    private float _jumpTime;
    private float _lastVelY;
    public bool CanJump() { return !_isJumping; }
    public bool CanLand() { return (_jumpTime > 0.1f); }
    public bool JumpEnds() { return (!IsJumping()); }

    private Player _player;

    // Boost Attributes
    [SerializeField]
    private int _boost; // -1 (left) | 0 (none) | 1 (right) | 2 (both)
    [SerializeField]
    private float _boostStr;
    [SerializeField]
    private float _gravity;

    // Fall & WallFall Systems
    private PlayerMovement _movement;
    private PlayerFall _fall;

    // Unity
    private void OnEnable(){
        RightDetector.OnSafeBoost += (bool wall) => { _wallFree = wall; };
    }
    private void OnDisable(){
        RightDetector.OnSafeBoost -= (bool wall) => { _wallFree = wall; };
    }

    // Unity
    void Awake(){
        LoadState();
        ////////////
        _isJumping = false;

        _jumpStr = 200.0f;
        _jumpTime = 0.0f;
        _lastVelY = 0.0f;

        _boost = 0;
        _boostStr = 150.0f;
        _gravity = 0.5f;

        _fall = GetComponent<PlayerFall>();
        _movement = GetComponent<PlayerMovement>();
        _player = GetComponent<Player>();
    }

    // Unity
    void FixedUpdate(){
        if (!_fall.Grounded()) _jumpTime += Time.deltaTime;
                          else _jumpTime = 0.0f;
    }
    
    // PlayerJump.cs <Jump>
    private bool PeakReached(){
        bool reached = ((_lastVelY * _body.velocity.y) < 0);
        _lastVelY = _body.velocity.y;
        return (reached && CanLand());
    }

    private void SetJumpGravity(){
        _body.gravityScale = _gravity;
    }

    public void DecideBoosts()
    {
        _boost = 2;
        if (_body.velocity.x < -1.0f) _boost = 1;
        if (_body.velocity.x > 1.0f) _boost = -1;
    }

    private void Jump(float force, float xforce = 0.0f) {
        SetJumpGravity();

        _lastVelY = 0.0f;

        DecideBoosts();

        if (_boost != 2) force = force * 0.9f;
        _body.AddForce(new Vector2(xforce, force));

        _body.velocity = new Vector2(_body.velocity.x * 0.75f, _body.velocity.y);

        _isJumping = true;
    }

    private void StartJump(float force) {
        _body.velocity = new Vector2(_body.velocity.x, 0.1f);
        if ((!_fall.Grounded()) && (_fall.IsFalling()) && (_fall.CanCoyoteJump())) {
            _body.velocity = new Vector3(_body.velocity.x * 2.5f, _body.velocity.y);
            _body.velocity = new Vector2(Mathf.Clamp(_body.velocity.x, -3.5f, 3.5f), _body.velocity.y);
            Jump(force);
        } else if (_fall.OnTheWall()) {
            float v = (force * 0.75f) * (_fall.FacingWall() ? -transform.right.x : transform.right.x);
            Jump(force * 0.9f, v);
            // Rotaci�n
            if (v > 0.0f) transform.localEulerAngles = new Vector2(0.0f, 0.0f);
            if (v < 0.0f) transform.localEulerAngles = new Vector2(0.0f, 180.0f);
            _boost = 0;
        } else { Jump(force); }
        OnJump?.Invoke();
    }

    private void EndJump(){ 
        _isJumping = false; 
    }

    // PlayerJump.cs <Boost>
    private bool RightBoost(){ return ((_boost > 0) || (_boost == 2)); }
    private bool LeftBoost(){ return ((_boost < 0) || (_boost == 2)); }
    public void ResetBoost(){ _boost = 0; }

    public void Boost(int side){
        _body.AddForce(new Vector2(_boostStr * side, 0.0f));
        _boost = 0;
    }

    public void CheckBoost(){
        if (_fall.Grounded() || !_wallFree) return;

        if (Input().Left()) if (LeftBoost()) Boost(-1);
        if (Input().Right()) if (RightBoost()) Boost(1);
    }

    // IHaveStates
    public void OnEnterState(){
        EnableSystem();
        ///////////////
        StartJump(_jumpStr);
        foreach (GameObject go in GameObject.FindGameObjectsWithTag("FaddedGround")) go.GetComponent<FadedGround>().DisableCol();
        MusicPlayer.Instance.PlayFX("Player_Jump_1/Player_Jump", 1f);
        _animator.SetBool("Jump", true);
    }

    public void OnExitState(){
        _animator.SetBool("Jump", false);
        EndJump();
        ////////////////
        DisableSystem();
    }

    public void OnState() {
        if (!IsEnabled()) return;
        /////////////////////////
        if (PeakReached()) EndJump();
        if (IsJumping()) CheckBoost();
    }

}