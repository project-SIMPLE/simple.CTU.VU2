using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pet_AI : MonoBehaviour
{
    private Animator _animator;
    // public Vector3 stopPosition { get; private set; }
    [Header("Di chuyển")]
    public float moveSpeed = 0.1f;
    public float walkCounter;
    public float waitCounter;
    public bool isWalking;

    private int _walkDirection;
    private float _waitTime;
    private float _walkTime;

    [Header("Mổ Thóc")]
    private bool _isPecking;
    private float _peckDuration;   // thời gian mổ
    private float _peckCounter;

    private void Start()
    {
        _animator = GetComponent<Animator>();

        _walkTime = Random.Range(3, 6);
        _waitTime = Random.Range(5, 7);

        waitCounter = _waitTime;
        walkCounter = _walkTime;

        ChooseDirection();
    }

    private void Update()
    {
        if (isWalking)
        {
            _animator.SetBool("isRunning", true);
            _animator.SetInteger("animation", 2);

            walkCounter -= Time.deltaTime;

            switch (_walkDirection)
            {
                case 0:
                    transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
                    WalkDirection();
                    break;
                case 1:
                    transform.localRotation = Quaternion.Euler(0f, 90, 0f);
                    WalkDirection();
                    break;
                case 2:
                    transform.localRotation = Quaternion.Euler(0f, -90, 0f);
                    WalkDirection();
                    break;
                case 3:
                    transform.localRotation = Quaternion.Euler(0f, 180, 0f);
                    WalkDirection();
                    break;
            }

            if (walkCounter <= 0)
            {
                // Dừng đi bộ
                isWalking = false;
                _animator.SetBool("isRunning", false);
                _animator.SetInteger("animation", 0);
                waitCounter = _waitTime;
                // stopPosition = transform.position;
            }
        }
        else if (_isPecking)
        {
            _peckCounter -= Time.deltaTime;
            if (_peckCounter <= 0)
            {
                _isPecking = false;
                _animator.SetInteger("animation", 0);
                waitCounter = _waitTime; // reset thời gian chờ
            }
        }
        else
        {
            // ---- Wait time ----
            waitCounter -= Time.deltaTime;

            if (waitCounter <= 0)
            {
                // Random mổ or đi tiếp
                if (Random.value < 0.4f) // 40% mổ thóc
                {
                    StartPecking();
                }
                else
                {
                    ChooseDirection();
                }
            }
        }
    }

    private void ChooseDirection()
    {
        _walkDirection = Random.Range(0, 3);

        isWalking = true;
        walkCounter = _walkTime;
    }

    private void StartPecking()
    {
        _isPecking = true;
        _peckDuration = Random.Range(2f, 3f); // 2s - 3s 
        _peckCounter = _peckDuration;

        _animator.SetInteger("animation", 4);
        // _animator.SetBool("isPecking", false);
        // Nên tạo audio vào đây
    }

    private void WalkDirection()
    {
        // Bắn ray dài 0.5f phía trước
        if (!Physics.Raycast(transform.position, transform.forward, 0.15f))
        {
            // Không va chạm → đi tiếp
            transform.position += transform.forward * (moveSpeed * Time.deltaTime);
        }
        else
        {
            // Va chạm → đổi hướng
            isWalking = false; // dừng đi bộ
            waitCounter = _waitTime;
            ChooseDirection(); // chọn hướng mới
        }
    }

}
