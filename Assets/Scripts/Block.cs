using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    // ����� ��� ��������, ���° index����, x,y������ �� �� °���� �˾ƾ��Ѵ�.
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
    ANIMAL id;

    protected SpriteRenderer spriteRenderer;
       
    public void Setup(int index, int x, int y)
    {
        info = new Info() { index = index, x = x, y = y };
        name = info.ToString();
        Change();
    }

    public virtual void Change()
    {
        ANIMAL[] ids = (ANIMAL[])System.Enum.GetValues(typeof(ANIMAL));
        ANIMAL randomID = ids[Random.Range(0, ids.Length)];
        Change(randomID);
    }
    private void Change(ANIMAL id)
    {
        this.id = id;
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.sprite = GameManager.Instance.GetSprite(id);
        spriteRenderer.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
    }

    public bool MatchBlock(Block block)
    {
        int mine = GetID();
        int other = block.GetID();

        if (mine + other < 0)
            return false;
        return mine == other;
    }
    protected virtual int GetID()
    {
        return (int)id;
    }

    bool isPressed;             // ����� ���ȴ��� ����
    Vector2 pressPoint;         // ����� ���� ��ġ

    protected virtual void OnMouseDown()
    {
        if(GameManager.Instance.IsLockBlock)
            return;

        isPressed = true;
        pressPoint = Camera.main.ScreenToWorldPoint(Input.mousePosition);
    }
    protected virtual void OnMouseDrag()
    {
        if (GameManager.Instance.IsLockBlock || !isPressed)
            return;
        
        // Press�� ��ġ�� ���� ��ġ�� ���� ������ �Ǵ��ϰ� Swap�� ��û�Ѵ�.
        Vector2 current = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        if(Vector2.Distance(pressPoint, current) >= 0.5f)
        {
            isPressed = false;
            Vector2 dir = current - pressPoint;
            if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
                GameManager.Instance.SlideBlock(info.index, dir.x < 0 ? VECTOR.LEFT : VECTOR.RIGHT);
            else
                GameManager.Instance.SlideBlock(info.index, dir.y < 0 ? VECTOR.DOWN : VECTOR.UP); 
        }
    }        
    protected virtual void OnMouseUp()
    {
        isPressed = false;
    }
}
