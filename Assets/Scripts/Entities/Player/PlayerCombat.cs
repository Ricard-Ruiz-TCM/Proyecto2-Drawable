﻿using System;
using UnityEngine;
using System.Collections;

public enum COMBAT_STATE {
    C_MELEE, C_RANGED
}

public class PlayerCombat : PlayerState, ICombat, IHaveStates {

    public static event Action OnAttack;
    public static event Action OnEndAttack;

    private bool _attacking;
    public bool IsAttacking() { return _attacking; }
    public bool AttackEnds() { return (!IsAttacking()); }
    
    // Attack Control
    protected float _attackTime;

    // Combat State rel. Weapon
    [SerializeField]
    private COMBAT_STATE _state;
    public COMBAT_STATE CombatState() { return _state; }
    public bool Melee() { return (CombatState() == COMBAT_STATE.C_MELEE); }
    public bool Ranged() { return (CombatState() == COMBAT_STATE.C_RANGED); }

    // Active Weapon
    [SerializeField]
    protected Attack _weapon;

    // Attacks
    private Attack _melee;
    private Attack _ranged;

    // Lyaer
    [SerializeField]
    private LayerMask _layer_Enemy;

    [SerializeField]
    private GameObject _inkBullet;
    private GameObject _container;

    // PlayerSateMachine
    protected Player _player;

    private bool _canAttack;

    void Awake(){
        LoadState();
        /////////////
        _attacking = false;
        _attackTime = 0.0f;

        _state = COMBAT_STATE.C_MELEE;

        _melee = Resources.Load<Attack>("ScriptableObjects/Attacks/MeleeAttack");
        _ranged = Resources.Load<Attack>("ScriptableObjects/Attacks/RangedAttack");
        _weapon = _melee;

        _layer_Enemy = LayerMask.GetMask("Enemy", "Object");

        _inkBullet = Resources.Load<GameObject>("Prefabs/Player/PlayerBullet");
        _container = GameObject.FindObjectOfType<ElementsContainer>().gameObject;

        _player = GetComponent<Player>();
        _animator = GetComponent<Animator>();
    }

    // PlayerCombat.cs <Combat>
    public void ChangeState() {
        if (CombatState() == COMBAT_STATE.C_MELEE) _state = COMBAT_STATE.C_RANGED;
                                              else _state = COMBAT_STATE.C_MELEE;

        if (Melee()) _weapon = _melee;
        if (Ranged()) _weapon = _ranged;
    }

    private void EndAttack() {
        _attacking = false;
        OnEndAttack?.Invoke();
    }

    // PlayerCombat.cs <Melee>
    private void MeleeAttack() {
        _body.velocity = Vector2.zero;
        _body.AddForce(new Vector2(transform.right.x * 75.0f, 37.5f));
        StartCoroutine(AttackDelay(0.3f));
        MusicPlayer.Instance.PlayFX("Player_AtkMelee/Player_AtkMelee_" + ((int)UnityEngine.Random.Range(1, 4)).ToString(), 0.5f);
    }

    // PlayerCombat.cs <Ranged>
    private void RangedAttack(){
        if (_player.Ink() < _weapon.InkCost) return;
        _player.UseInk(_weapon.InkCost);
        GameObject bullet = Instantiate(_inkBullet, new Vector2(this.transform.position.x + 0.005f, this.transform.position.y + 0.5f), Quaternion.identity, _container.transform);
        bullet.GetComponent<PlayerBullet>().Dir(this.transform.right.x);
        MusicPlayer.Instance.PlayFX("Player_AtkDistance/Player_AtkDistance_" + ((int)UnityEngine.Random.Range(1, 2)).ToString(), 0.5f);
    }

    // ICombat
    public void Attack(ICombat target){
        _attackTime = 0.0f;
        _attacking = true;
        OnAttack?.Invoke();
        if (Ranged()) RangedAttack();
        if (Melee()) MeleeAttack();
    }

    public void TakeDamage(Attack weapon){ _player.TakeDamage(weapon.Damage);
    }

    // IHaveStates
    public void OnEnterState(){
        EnableSystem();
        ///////////////
        Attack(null);
        _canAttack = true;
        if (Melee())
        {
            _animator.SetBool("Melee", true);
            ParticleInstancer.Instance.StartParticles("RafagaMelee_Particle", transform.Find("MeleePos").transform);

        }
        if (Ranged()) _animator.SetBool("Ranged", true);
    }

    public void OnExitState(){
        _animator.SetBool("Melee", false);
        _animator.SetBool("Ranged", false);
        ////////////////
        DisableSystem();
        OnEndAttack?.Invoke();
        //StopAllCoroutines();
    }

    public void OnState(){
        if (!IsEnabled()) return;

        if (Melee() && (_canAttack)) {
            _canAttack = false;
            foreach(GameObject go in GameObject.FindGameObjectsWithTag("Enemy")){
                if (Vector2.Distance(go.transform.position, this.transform.position) < _weapon.Range * 1.5f){
                    Vector2 dir = (transform.position - go.transform.position);
                    dir.Normalize();
                    if (go.GetComponent<ICombat>() == null) break;
                    if ((dir.x < 0) && (transform.right.x > 0)) go.GetComponent<ICombat>().TakeDamage(_weapon);
                    else if ((dir.x > 0) && (transform.right.x < 0)) go.GetComponent<ICombat>().TakeDamage(_weapon);
                    else if (dir.y < 0) go.GetComponent<ICombat>().TakeDamage(_weapon);
                    MusicPlayer.Instance.PlayFX("Player_AtkMelee_Hit/Player_AtkMelee_Hit_" + ((int)UnityEngine.Random.Range(1, 4)).ToString(), 0.5f);
                }
            }
            foreach (GameObject go in GameObject.FindGameObjectsWithTag("Boss"))
            {
                if (Vector2.Distance(go.transform.position, this.transform.position) < _weapon.Range * 2.0f){
                    Vector2 dir = (transform.position - go.transform.position);
                    dir.Normalize();
                    if (go.GetComponent<BossAttack>() == null) break;
                    if ((dir.x < 0) && (transform.right.x > 0)) go.GetComponent<BossAttack>().TakeDamage(_weapon);
                    else if ((dir.x > 0) && (transform.right.x < 0)) go.GetComponent<BossAttack>().TakeDamage(_weapon);
                    else if (dir.y < 0) go.GetComponent<BossAttack>().TakeDamage(_weapon);
                }
            }
        }
        _attackTime += Time.deltaTime;
        if (_attackTime >= _weapon.Cooldown) EndAttack();
    }


    private IEnumerator AttackDelay(float time)
    {
        yield return new WaitForSeconds(time);
    }
}
