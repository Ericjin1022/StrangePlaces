using UnityEngine;
using StrangePlaces.Level3_ColorSwap;

namespace StrangePlaces.DemoQuantumCollapse
{
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(PlayerController2D))]
    public class PlayerAnimation2D : MonoBehaviour
    {
        private Animator _animator;
        private SpriteRenderer _spriteRenderer;
        
        // 核心脚本引用
        private PlayerController2D _playerController;
        private PlayerColorSwap2D _colorSwap;
        
        // 缓存参数 Hash，比用字符串名字设置参数性能更好
        private readonly int _animSpeed = Animator.StringToHash("Speed");
        private readonly int _animIsJumping = Animator.StringToHash("IsJumping");
        private readonly int _animIsWhite = Animator.StringToHash("IsWhite");

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            _spriteRenderer = GetComponent<SpriteRenderer>();
            
            _playerController = GetComponent<PlayerController2D>();
            
            // 颜色切换脚本不一定在每一个关卡都有，所以安全获取
            _colorSwap = GetComponent<PlayerColorSwap2D>(); 
        }

        private void Update()
        {
            // --- 1. 面向朝向翻转 (Flip) ---
            // 获取水平输入轴 (A/D 键)，如果大于 0 往右走，小于 0 往左走
            float moveInput = _playerController.MoveAxis;
            
            if (moveInput > 0.01f)
            {
                _spriteRenderer.flipX = false; // 朝右不翻转
            }
            else if (moveInput < -0.01f)
            {
                _spriteRenderer.flipX = true;  // 朝左翻转贴图
            }

            // --- 2. 传递速度 (Speed) 给状态机 ---
            // 使用 Mathf.Abs 取绝对值，因为向左走 moveInput 是负数，但动画机只看绝对值
            _animator.SetFloat(_animSpeed, Mathf.Abs(moveInput));

            // --- 3. 传递跳跃状态 (IsJumping) 给状态机 ---
            // 原先跳跃依赖于不在地面上，所以直接用 !isG
            bool shouldJump = !_playerController.isG;
            if (_animator.GetBool(_animIsJumping) != shouldJump)
            {
                Debug.Log($"<color=cyan>[Animator] 状态变化 IsJumping 变为了: {shouldJump}</color>");
            }
            _animator.SetBool(_animIsJumping, shouldJump);

            // --- 4. 传递黑白状态 (IsWhite) 给状态机 ---
            if (_colorSwap != null)
            {
                // 当前状态是否是白色
                bool isCurrentlyWhite = _colorSwap.CurrentColor == BinaryColor.White; 
                _animator.SetBool(_animIsWhite, isCurrentlyWhite);
            }
        }
    }
}
