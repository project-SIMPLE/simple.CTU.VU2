using UnityEngine;

public class Thuan_23127_DuckAiAction : MonoBehaviour
{
    private Animator _animator;
    public float moveSpeed = 0.2f;
    private Vector3 _stopPosition;
    private float _walkTime;
    public float walkCounter;
    private float _waitTime;
    public float waitCounter;
    private int _walkDirection;
 
    public bool isWalking;

    private void Start()
    {
        _animator = GetComponent<Animator>();
 
        _walkTime = Random.Range(3,6);
        _waitTime = Random.Range(5,7);
 
 
        waitCounter = _waitTime;
        walkCounter = _walkTime;
 
        ChooseDirection();
    }

    private void Update()
    {
        if (isWalking)
        {
            _animator.SetBool("isRunning", true);
 
            walkCounter -= Time.deltaTime;
 
            switch (_walkDirection)
            {
                case  0:
                    transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
                    transform.position += transform.forward * (moveSpeed * Time.deltaTime);
                    break;
                case  1:
                    transform.localRotation = Quaternion.Euler(0f, 90, 0f);
                    transform.position += transform.forward * (moveSpeed * Time.deltaTime);
                    break;
                case  2:
                    transform.localRotation = Quaternion.Euler(0f, -90, 0f);
                    transform.position += transform.forward * (moveSpeed * Time.deltaTime);
                    break;
                case  3:
                    transform.localRotation = Quaternion.Euler(0f, 180, 0f);
                    transform.position += transform.forward * (moveSpeed * Time.deltaTime);
                    break;
            }

            if (!(walkCounter <= 0)) return;
            _stopPosition = new Vector3(transform.position.x, transform.position.y, transform.position.z);
            isWalking = false;
            //stop movement
            transform.position = _stopPosition;
            _animator.SetBool("isRunning", false);
            //reset the waitCounter
            waitCounter = _waitTime;
        }
        else
        {
            waitCounter -= Time.deltaTime;
 
            if (waitCounter <= 0)
            {
                ChooseDirection();
            }
        }
    }

    private void ChooseDirection()
    {
        _walkDirection = Random.Range(0, 3);
 
        isWalking = true;
        walkCounter = _walkTime;
    }
}
