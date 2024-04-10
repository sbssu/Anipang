using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum VECTOR
{
    UP,
    DOWN,
    LEFT,
    RIGHT,
}

public class BlockPanel : MonoBehaviour
{
    private class BlockGroup
    {
        public Block[] blocks;      // ����� ��� �迭.
        public bool isHorizontal;   // �������� ��������.

        public int Length => blocks.Length;
    }

    public static BlockPanel Instance { get; private set; }

    const int BLOCK_WIDTH = 6;
    const int BLOCK_HEIGHT = 6;

    [SerializeField] Block blockPrefab;

    public bool IsLockBlock;
    Block[] blocks;

    private void Awake()
    {
        Instance = this;
    }
    private void Start()
    {
        GenerateBlock();
    }

    [ContextMenu("���ο� ��� �����")]
    public void GenerateBlock()
    {
        // ��� �迭�� ������ ������ ���� �����Ѵ�.
        if (blocks == null)
            blocks = new Block[BLOCK_WIDTH * BLOCK_HEIGHT];

        // ��ϵ��� �����ϱ� �� ���� ��ϵ��� ����
        for (int i = 0; i < blocks.Length; i++)
        {
            if (blocks[i] != null)
                DestroyImmediate(blocks[i].gameObject);
        }

        // �迭�� ������� ���� ����� ã�� ������ �󿡼� ����
        Block[] anotherBlocks = FindObjectsOfType<Block>();
        foreach (Block block in anotherBlocks)
            DestroyImmediate(block.gameObject);

        // ��� ����.
        Vector3 pivot = new Vector3((BLOCK_WIDTH / 2 - 0.5f) * -1f, BLOCK_HEIGHT / 2 - 0.5f, 0);
        for (int i = 0; i < blocks.Length; i++)
        {
            // ����� ���� ������ �� ������ �����Ѵ�.
            Block block = Instantiate(blockPrefab, transform);
            block.Setup(i, i % BLOCK_WIDTH, i / BLOCK_WIDTH);

            // ����� ��ġ�� ũ�⸦ �����Ѵ�.
            block.transform.localPosition = pivot + new Vector3(block.info.x, -block.info.y);
            block.transform.localScale = Vector3.one;
            blocks[i] = block;
        }

        // �ߺ� ��� üũ.
        foreach (Block block in blocks)
        {
            for (int i = 0; i < 2; i++)
            {
                while (true)
                {
                    BlockGroup group = SearchGroup(block, i == 0);
                    if (group.Length >= 3)
                    {
                        block.Change();
                    }
                    else
                        break;
                }
            }
        }
    }

    // ����� ��� ��������
    private BlockGroup SearchGroup(Block block, bool isHorizontal)
    {
        // �ּ�, �ִ� �ε����� ���ذ��� ��.
        int minIndex = isHorizontal ? block.info.y * BLOCK_WIDTH : block.info.x;
        int maxIndex = isHorizontal ? minIndex + BLOCK_WIDTH - 1 : minIndex + (BLOCK_WIDTH * (BLOCK_HEIGHT - 1));
        int addValue = isHorizontal ? 1 : BLOCK_WIDTH;

        List<Block> result = new List<Block>();
        for (int index = minIndex; index <= maxIndex; index += addValue)
        {
            List<Block> check = new List<Block>();
            for (int i = index; i <= maxIndex; i += addValue)
            {
                if (block.id == blocks[i].id)
                    check.Add(blocks[i]);
                else
                    break;
            }

            if (check.Contains(block) && check.Count > result.Count)
                result = check;
        }

        // ����� BlockGroup���� ��ȯ.
        BlockGroup group = new BlockGroup();
        group.blocks = result.ToArray();
        group.isHorizontal = isHorizontal;

        return group;
    }
    private bool MatchBlockAfterSwap(params Block[] matechBlocks)
    {
        List<Block> removeList = new List<Block>();
        foreach(Block block in matechBlocks)
        {
            BlockGroup horizontalGroup = SearchGroup(block, true);
            BlockGroup verticalGroup = SearchGroup(block, false);

            if(horizontalGroup.Length >= 3)
            {
                removeList.AddRange(horizontalGroup.blocks);
            }
            if(verticalGroup.Length >= 3)
            {
                removeList.AddRange(verticalGroup.blocks);
            }
        }

        // ������ ����� �ּ� 3�� �̻��� ��� ��ġ ����.
        bool isMatch = removeList.Count >= 3;
        if(isMatch)
        {
            // IEnumerable.Distinct() : �ߺ� ����.
            foreach (Block removeBlock in removeList.Distinct())
            {
                blocks[removeBlock.info.index] = null;  // �迭���� ����.
                Destroy(removeBlock.gameObject);        // ���� ���� ������Ʈ ����.
            }
        }
        return isMatch;
    }
    public bool SwapBlock(int index, VECTOR dir)
    {
        int targetIndex = index;
        targetIndex += dir switch
        {
            VECTOR.UP => -BLOCK_WIDTH,
            VECTOR.DOWN => BLOCK_WIDTH,
            VECTOR.LEFT => -1,
            VECTOR.RIGHT => 1,
            _ => 0
        };

        // ������ ����� ���.
        if (targetIndex < 0 || targetIndex >= blocks.Length)
            return false;

        // ���� ������ �ƴ� ���.
        if (dir == VECTOR.LEFT || dir == VECTOR.RIGHT)
        {
            if (blocks[index].info.y != blocks[targetIndex].info.y)
                return false;
        }

        StartCoroutine(IECheckSwap(index, targetIndex));
        return true;
    }
    
    IEnumerator IECheckSwap(int index, int targetIndex)
    {
        IsLockBlock = true;
        Block block = blocks[index];
        Block targetBlock = blocks[targetIndex];
        yield return StartCoroutine(IESwap(block, targetBlock));        // ������ ���������� ����Ѵ�.

        // ��ġ�� ���� �ʾҴٸ� �ٽ� ������ �Ѵ�.
        if(MatchBlockAfterSwap(block, targetBlock) == false)
            yield return StartCoroutine(IESwap(block, targetBlock));

        IsLockBlock = false;
    }
    IEnumerator IESwap(Block blockA, Block blockB)
    {
        const float SWAP_SPEED = 5f;

        // ��ȯ
        Vector3 posA = blockB.transform.localPosition;
        Vector3 posB = blockA.transform.localPosition;
        while (blockA.transform.localPosition != posA || blockB.transform.localPosition != posB)
        {
            blockA.transform.localPosition = Vector3.MoveTowards(blockA.transform.localPosition, posA, SWAP_SPEED * Time.deltaTime);
            blockB.transform.localPosition = Vector3.MoveTowards(blockB.transform.localPosition, posB, SWAP_SPEED * Time.deltaTime);
            yield return null;
        }

        // blocks �迭 ���� ��ȯ.
        int indexA = blockA.info.index;
        int indexB = blockB.info.index;
        blocks[indexA] = blockB;
        blocks[indexB] = blockA;

        // block���� info ��ȯ.
        Block.Info tempInfo = blockA.info;
        blockA.info = blockB.info;
        blockB.info = tempInfo;

        blockA.name = blockA.info.ToString();
        blockB.name = blockB.info.ToString();
    }
}
