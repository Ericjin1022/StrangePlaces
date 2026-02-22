using UnityEngine;
using StrangePlaces.Level3_ColorSwap;

namespace StrangePlaces.DemoQuantumCollapse
{
    [RequireComponent(typeof(PlayerController2D))]
    public class PlayerAnimation2D : MonoBehaviour
    {
        [Header("Dual Animators")]
        [Tooltip("负责播放黑色状态动画的 Animator (挂载在 Body_Black)")]
        public Animator animatorBlack;
        [Tooltip("负责播放白色状态动画的 Animator (挂载在 Body_White)")]
        public Animator animatorWhite;

        [Header("Dual SpriteRenderers")]
        public SpriteRenderer spriteRendererBlack;
        public SpriteRenderer spriteRendererWhite;
        
        // 核心脚本引用
        private PlayerController2D _playerController;
        private PlayerColorSwap2D _colorSwap;
        
        // 缓存参数 Hash
        private readonly int _animSpeed = Animator.StringToHash("Speed");
        private readonly int _animIsJumping = Animator.StringToHash("IsJumping");
        // 注意：现在不需要传 IsWhite 给单个 Animator 了，因为黑白分开了，各自状态机内只有跳跃和移动

        private void Awake()
        {
            _playerController = GetComponent<PlayerController2D>();
            _colorSwap = GetComponent<PlayerColorSwap2D>(); 
            
            // 兼容防呆：如果没拖拽，尝试从子物体获取
            if (animatorBlack == null || animatorWhite == null)
            {
                Animator[] anims = GetComponentsInChildren<Animator>();
                if (anims.Length >= 2)
                {
                    animatorBlack = anims[0];
                    animatorWhite = anims[1];
                }
            }

            if (spriteRendererBlack == null || spriteRendererWhite == null)
            {
                SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>();
                if (renderers.Length >= 2)
                {
                    spriteRendererBlack = renderers[0];
                    spriteRendererWhite = renderers[1];
                }
            }
        }

        private void Update()
        {
            // --- 1. 面向朝向翻转 (Flip) ---
            float moveInput = _playerController.MoveAxis;
            
            if (moveInput > 0.01f)
            {
                if (spriteRendererBlack) spriteRendererBlack.flipX = false;
                if (spriteRendererWhite) spriteRendererWhite.flipX = false;
            }
            else if (moveInput < -0.01f)
            {
                if (spriteRendererBlack) spriteRendererBlack.flipX = true;
                if (spriteRendererWhite) spriteRendererWhite.flipX = true;
            }

            // --- 2. 传递速度 (Speed) 给双状态机 ---
            float speed = Mathf.Abs(moveInput);
            if (animatorBlack) animatorBlack.SetFloat(_animSpeed, speed);
            if (animatorWhite) animatorWhite.SetFloat(_animSpeed, speed);

            // --- 3. 传递跳跃状态 (IsJumping) 给双状态机 ---
            bool shouldJump = !_playerController.isG;
            if (animatorBlack) animatorBlack.SetBool(_animIsJumping, shouldJump);
            if (animatorWhite) animatorWhite.SetBool(_animIsJumping, shouldJump);
        }
    }
}
