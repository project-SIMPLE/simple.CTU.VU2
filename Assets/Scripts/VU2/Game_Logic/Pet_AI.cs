using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// =============================================================================
// Pet_AI - Simple AI behavior for farm animals (chickens, ducks).
// Pet_AI - Hành vi AI đơn giản cho vật nuôi nông trại (gà, vịt).
// 
// This script controls autonomous pet behavior:
// - Random walking in 4 directions
// - Pecking animation (eating)
// - Collision avoidance via raycasting
// 
// Script này điều khiển hành vi tự động của thú cưng:
// - Đi bộ ngẫu nhiên theo 4 hướng
// - Animation mổ thóc (ăn)
// - Tránh va chạm qua raycasting
// 
// Behavior loop:
// 1. Walk for 3-6 seconds in random direction
// 2. Stop and wait for 5-7 seconds
// 3. 40% chance to peck, 60% chance to walk again
// 4. Repeat
// =============================================================================
public class Pet_AI : MonoBehaviour
{
    // =========================================================================
    // REFERENCES
    // THAM CHIẾU
    // =========================================================================
    // Animator for controlling pet animations.
    // Animator để điều khiển animation thú cưng.
    public Animator _animator;

    // =========================================================================
    // MOVEMENT CONFIGURATION
    // CẤU HÌNH DI CHUYỂN
    // =========================================================================
    [Header("Movement / Di chuyển")]
    // Speed of walking movement.
    // Tốc độ di chuyển khi đi bộ.
    public float moveSpeed = 0.1f;
    
    // Countdown timer for walking duration.
    // Bộ đếm thời gian cho thời lượng đi bộ.
    public float walkCounter;
    
    // Countdown timer for waiting duration.
    // Bộ đếm thời gian cho thời lượng chờ.
    public float waitCounter;
    
    // True when pet is currently walking.
    // True khi thú cưng đang đi bộ.
    public bool isWalking;

    // =========================================================================
    // INTERNAL STATE
    // TRẠNG THÁI NỘI BỘ
    // =========================================================================
    
    // Current walking direction: 0=forward, 1=right, 2=left, 3=back.
    // Hướng đi hiện tại: 0=trước, 1=phải, 2=trái, 3=sau.
    private int _walkDirection;
    
    // Base wait time (randomized 5-7 seconds).
    // Thời gian chờ gốc (ngẫu nhiên 5-7 giây).
    private float _waitTime;
    
    // Base walk time (randomized 3-6 seconds).
    // Thời gian đi gốc (ngẫu nhiên 3-6 giây).
    private float _walkTime;

    // =========================================================================
    // PECKING BEHAVIOR
    // HÀNH VI MỔ THÓC
    // =========================================================================
    [Header("Pecking / Mổ Thóc")]
    // True when pet is currently pecking.
    // True khi thú cưng đang mổ thóc.
    private bool _isPecking;
    
    // Duration of pecking animation.
    // Thời lượng animation mổ.
    private float _peckDuration;
    
    // Countdown timer for pecking.
    // Bộ đếm thời gian cho mổ.
    private float _peckCounter;

    // =========================================================================
    // Start - Initialize AI parameters.
    // Start - Khởi tạo các tham số AI.
    // =========================================================================
    private void Start()
    {
        // Auto-find Animator if not assigned.
        // Tự động tìm Animator nếu chưa gán.
        if (_animator == null)
        {
            _animator = GetComponent<Animator>();
        }

        // Randomize walk and wait times.
        // Ngẫu nhiên hóa thời gian đi và chờ.
        _walkTime = Random.Range(3, 6);
        _waitTime = Random.Range(5, 7);

        waitCounter = _waitTime;
        walkCounter = _walkTime;

        ChooseDirection();
    }

    // =========================================================================
    // Update - Main AI loop handling walking, waiting, and pecking.
    // Update - Vòng lặp chính AI xử lý đi bộ, chờ, và mổ.
    // =========================================================================
    private void Update()
    {
        if (isWalking)
        {
            // Playing walk animation.
            // Phát animation đi bộ.
            _animator.SetBool("isRunning", true);
            _animator.SetInteger("animation", 2);

            walkCounter -= Time.deltaTime;

            // Set rotation based on direction and move.
            // Đặt rotation dựa trên hướng và di chuyển.
            switch (_walkDirection)
            {
                case 0:  // Forward / Trước
                    transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
                    WalkDirection();
                    break;
                case 1:  // Right / Phải
                    transform.localRotation = Quaternion.Euler(0f, 90, 0f);
                    WalkDirection();
                    break;
                case 2:  // Left / Trái
                    transform.localRotation = Quaternion.Euler(0f, -90, 0f);
                    WalkDirection();
                    break;
                case 3:  // Back / Sau
                    transform.localRotation = Quaternion.Euler(0f, 180, 0f);
                    WalkDirection();
                    break;
            }

            // Transition to waiting state when walk time expires.
            // Chuyển sang trạng thái chờ khi hết thời gian đi.
            if (walkCounter <= 0)
            {
                isWalking = false;
                _animator.SetBool("isRunning", false);
                _animator.SetInteger("animation", 0);
                waitCounter = _waitTime;
            }
        }
        else if (_isPecking)
        {
            // Handle pecking countdown.
            // Xử lý đếm ngược mổ.
            _peckCounter -= Time.deltaTime;
            if (_peckCounter <= 0)
            {
                _isPecking = false;
                _animator.SetInteger("animation", 0);
                waitCounter = _waitTime;
            }
        }
        else
        {
            // Waiting state - countdown to next action.
            // Trạng thái chờ - đếm ngược đến hành động tiếp.
            waitCounter -= Time.deltaTime;

            if (waitCounter <= 0)
            {
                // 40% chance to peck, 60% chance to walk.
                // 40% cơ hội mổ, 60% cơ hội đi bộ.
                if (Random.value < 0.4f)
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

    // =========================================================================
    // ChooseDirection - Picks a random direction and starts walking.
    // ChooseDirection - Chọn hướng ngẫu nhiên và bắt đầu đi bộ.
    // =========================================================================
    private void ChooseDirection()
    {
        _walkDirection = Random.Range(0, 3);

        isWalking = true;
        walkCounter = _walkTime;
    }

    // =========================================================================
    // StartPecking - Starts the pecking animation for 2-3 seconds.
    // StartPecking - Bắt đầu animation mổ trong 2-3 giây.
    // =========================================================================
    private void StartPecking()
    {
        _isPecking = true;
        _peckDuration = Random.Range(2f, 3f);
        _peckCounter = _peckDuration;

        _animator.SetInteger("animation", 4);
    }

    // =========================================================================
    // WalkDirection - Moves forward if no obstacle, else changes direction.
    // WalkDirection - Di chuyển về phía trước nếu không có chướng ngại, ngược lại đổi hướng.
    // 
    // Uses raycast for collision detection.
    // Dùng raycast để phát hiện va chạm.
    // =========================================================================
    private void WalkDirection()
    {
        // Cast ray 0.15m forward to detect obstacles.
        // Bắn ray 0.15m về phía trước để phát hiện chướng ngại.
        if (!Physics.Raycast(transform.position, transform.forward, 0.15f))
        {
            // No collision - continue moving.
            // Không va chạm - tiếp tục di chuyển.
            transform.position += transform.forward * (moveSpeed * Time.deltaTime);
        }
        else
        {
            // Collision detected - stop and choose new direction.
            // Phát hiện va chạm - dừng và chọn hướng mới.
            isWalking = false;
            waitCounter = _waitTime;
            ChooseDirection();
        }
    }

}