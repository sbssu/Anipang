using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ANIMAL
{
    MONKEY,
    PANDA,
    PENGUIN,
    PIG,
    RABBIT,
    SNAKE,
    GIRAFFE,
}

public class Block : MonoBehaviour
{
    // 블록은 어떠한 동물인지, 몇번째 index인지, x,y축으로 몇 번 째인지 알아야한다.
    public ANIMAL id;
    public struct Info
    {
        public int index;
        public int x;
        public int y;

        public override string ToString()
        {
            return $"Block_{index} (x:{x},y:{y})";
        }
    }
        
    public Info info;

    private SpriteRenderer spriteRenderer;
       
    public void Setup(int index, int x, int y)
    {
        info = new Info() { index = index, x = x, y = y };
        name = info.ToString();
        Change();
    }
    public void Change()
    {
        ANIMAL[] ids = (ANIMAL[])System.Enum.GetValues(typeof(ANIMAL));
        ANIMAL randomID = ids[Random.Range(0, ids.Length)];
        Change(randomID);
    }
    public void Change(ANIMAL id)
    {
        this.id = id;
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.sprite = BlockPanel.Instance.GetSprite(id);
        spriteRenderer.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
    }

    
    bool isPressed;             // 블록이 눌렸는지 여부
    Vector2 pressPoint;         // 블록이 눌린 위치

    private void OnMouseDown()
    {
        if(BlockPanel.Instance.IsLockBlock)
            return;

        isPressed = true;
        pressPoint = Camera.main.ScreenToWorldPoint(Input.mousePosition);
    }
    private void OnMouseDrag()
    {
        if (BlockPanel.Instance.IsLockBlock || !isPressed)
            return;
        
        // Press한 위치와 현재 위치를 비교해 방향을 판단하고 Swap을 요청한다.
        Vector2 current = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        if(Vector2.Distance(pressPoint, current) >= 0.5f)
        {
            isPressed = false;
            Vector2 dir = current - pressPoint;
            if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
                BlockPanel.Instance.SlideBlock(info.index, dir.x < 0 ? VECTOR.LEFT : VECTOR.RIGHT);
            else
                BlockPanel.Instance.SlideBlock(info.index, dir.y < 0 ? VECTOR.DOWN : VECTOR.UP); 
        }
    }        
    private void OnMouseUp()
    {
        isPressed = false;
    }
}
