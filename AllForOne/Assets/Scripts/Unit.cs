﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;

public class Unit : MonoBehaviour
{
    [SerializeField]
    private Animator _animator;

    [SerializeField]
    private Text _timerText;

    [SerializeField]
    private Canvas _unitCanvas;

    [SerializeField]
    private Camera _playCamera;

    [SerializeField]
    private float _minClampY;

    [SerializeField]
    private float _maxClampY;

    [SerializeField]
    private AudioSource _punchSound;

    [SerializeField]
    private AudioSource _placedSound;

    [SerializeField]
    private AudioSource _deathSound;

    [SerializeField]
    private AudioSource _fortifySound;

    [SerializeField]
    private AudioSource _powerUpGrabbedSound;

    [SerializeField]
    private AudioSource _powerUpActivatedSound;

    [SerializeField]
    private List<GameObject> _player1Meshes;

    [SerializeField]
    private List<GameObject> _player2Meshes;

    [SerializeField]
    private List<Image> _powerUpImages;

    [SerializeField]
    private MeshRenderer _teamIndicator;

    [SerializeField]
    private Transform _rayPosition;

    [SerializeField]
    private Transform _fortifiedShark;

    [SerializeField]
    private LayerMask _attackLayer;

    [SerializeField]
    private LayerMask _powerUpLayer;

    [SerializeField]
    private float _attackCooldown;

    public float _health;
    public float _strenght;
    public float _speed;
    public float _defense;

    public bool _activated;
    public bool _isPlayer1;
    public bool _freeze;

    public int _currentMesh;

    private int _playTime = 10;

    private Color _blue = new Color32(42, 87, 226, 255);
    private Color _red = new Color32(226, 42, 42, 255);

    private List<PowerUp> _powerUps = new List<PowerUp>();

    private float _mouseSensitivity = 40;
    private float _movementSpeed = 1.5f;
    private float _yRotation;

    private bool _inAttackAnimation;
    private bool _isFortified;

    private void Start()
    {
        _placedSound.Play();

        if (_isPlayer1)
        {
            _player1Meshes[_currentMesh].SetActive(true);
            _teamIndicator.material.color = _red;
        }
        else
        {
            _player2Meshes[_currentMesh].SetActive(true);
            _teamIndicator.material.color = _blue;
        }
    }

    public void StartControl()
    {
        _isFortified = false;
        _fortifiedShark.gameObject.SetActive(false);

        _playCamera.enabled = true;
        _activated = true;
        _unitCanvas.enabled = true;

        StartCoroutine(UnitControlTimer());
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void Update()
    {
        if (!_activated)
        {
            return;
        }

        Rotation();

        if (Input.GetKeyDown(KeyCode.Mouse0) && !_inAttackAnimation)
        {
            Attack();
            _inAttackAnimation = true;
            Invoke("CooldownEnd", _attackCooldown);
        }

        if (Input.GetKeyDown(KeyCode.Mouse1))
        {
            Interact();
        }

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            UsePowerUp(0);
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            UsePowerUp(1);
        }

        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            UsePowerUp(2);
        }

        if (Input.GetKeyDown(KeyCode.F))
        {
            Fortify();
        }
    }

    private void FixedUpdate()
    {
        if (!_activated || _inAttackAnimation)
        {
            return;
        }

        Movement();
    }

    private IEnumerator UnitControlTimer()
    {
        for (int i = _playTime; i > 0;)
        {
            if (!_freeze)
            {
                i--;
            }

            if (_activated)
            {
                _timerText.text = i.ToString();
                yield return new WaitForSeconds(1);
            }
        }

        EndControlPhase();
    }

    private void Attack()
    {
        _animator.Play("Punch");
        _punchSound.Play();

        RaycastHit hit;
        if (Physics.Raycast(_rayPosition.position, _rayPosition.forward, out hit, 1.5f, _attackLayer))
        {
            Unit unit = hit.collider.GetComponent<Unit>();
            if (unit != null && unit._isPlayer1 != _isPlayer1)
            {
                unit.TakeDamage(_strenght);
            }
        }
    }

    private void Interact()
    {
        RaycastHit hit;
        if (Physics.Raycast(_rayPosition.position, _rayPosition.forward, out hit, 1.5f, _powerUpLayer))
        {
            if (_powerUps.Count == 3)
            {
                return;
            }

            _powerUpGrabbedSound.Play();
            PowerUp powerUp = hit.collider.GetComponent<PowerUp>();
            _powerUps.Add(powerUp);
            powerUp.gameObject.SetActive(false);
            _powerUpImages[_powerUps.Count - 1].sprite = powerUp._powerUpImage;
        }
    }

    private void UsePowerUp(int powerUpNum)
    {
        _powerUpActivatedSound.Play();

        if (powerUpNum < _powerUps.Count)
        {
            StartCoroutine(_powerUps[powerUpNum].Use(this));
            _powerUps.Remove(_powerUps[powerUpNum]);

            for (int i = 0; i < _powerUps.Count; i++)
            {
                _powerUpImages[i].sprite = _powerUps[i]._powerUpImage;
            }

            _powerUpImages[_powerUps.Count].sprite = null;
        }
    }

    private void Fortify()
    {
        _isFortified = true;
        _fortifySound.Play();
        _fortifiedShark.gameObject.SetActive(true);
        EndControlPhase();
    }

    private void CooldownEnd()
    {
        _inAttackAnimation = false;
    }

    private void EndControlPhase()
    {
        if (!_activated)
        {
            return;
        }

        Cursor.lockState = CursorLockMode.None;
        _playCamera.enabled = false;
        _activated = false;
        _unitCanvas.enabled = false;
        _animator.SetFloat("Speed", 0);
        _animator.Play("Idle");

        RaycastHit hit;
        if (Physics.Raycast(_rayPosition.position, -_rayPosition.up, out hit, Mathf.Infinity))
        {
            if (hit.collider.CompareTag("GroundUnsafe"))
            {
                Die();
            }
        }

        GameManager._instance.EndControlPhase();
    }

    private void Movement()
    {
        _animator.SetFloat("Speed",Mathf.RoundToInt(Input.GetAxisRaw("Vertical")));
        float vertical = Input.GetAxis("Vertical");

        transform.Translate(new Vector3(0, 0, vertical) * Time.deltaTime * (_movementSpeed + _speed));
    }

    private void Rotation()
    {
        float MouseX = Input.GetAxis("Mouse X");
        float MouseY = Input.GetAxis("Mouse Y");

        _yRotation += -MouseY;

        _yRotation = Mathf.Clamp(_yRotation, _minClampY, _maxClampY);

        transform.eulerAngles += new Vector3(0, MouseX, 0) * Time.deltaTime * _mouseSensitivity;
        _playCamera.transform.eulerAngles = new Vector3(_yRotation, transform.eulerAngles.y, 0);
    }

    public void TakeDamage(float damage)
    {
        if (_isFortified)
        {
            damage /= _defense;
        }

        _health -= damage;

        if (_health <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        _fortifiedShark.gameObject.SetActive(false);
        _animator.Play("Die");

        _deathSound.pitch = Random.Range(0.1f, 1.9f);
        _deathSound.Play();

        if (_isPlayer1)
        {
            GameManager._instance._player1Units--;
            if (GameManager._instance._player1Units == 0)
            {
                GameManager._instance.EndGame(true);
            }
        }
        else
        {
            GameManager._instance._player2Units--;
            if (GameManager._instance._player2Units == 0)
            {
                GameManager._instance.EndGame(false);
            }
        }

        GetComponent<Collider>().enabled = false;
        Destroy(this);
    }
}
