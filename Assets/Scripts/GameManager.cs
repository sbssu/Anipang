using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using UnityEngine;

public enum VECTOR
{
    UP,
    DOWN,
    LEFT,
    RIGHT,
}

public class GameManager : MonoBehaviour
{
    private class BlockGroup
    {
        public Block[] blocks;      // ����� ��� �迭.
        public bool isHorizontal;   // �������� ��������.

        public int Length => blocks.Length;
    }

    static GameManager instance;
    public static GameManager Instance
    {
        get
        {
            if (instance == null)
                instance = FindObjectOfType<GameManager>();
            return instance;
        }
    }

    const int BLOCK_WIDTH = 7;
    const int BLOCK_HEIGHT = 7;

    [SerializeField] Block blockPrefab;
    [SerializeField] GameUI gameUI;
    [SerializeField] AudioPlayer audioPlayer;
    [SerializeField] ParticleSystem popFxPrefab;
    [SerializeField] Sprite[] animalSprites;


    Block[] panelBlocks;

    const float MAX_TIME = 60f;     // 1��.
    float currentTime;              // ���� �ð�.
    int score;                      // ����.

    bool isLockTime;                // �ð��� �帧 ����.
    bool isLockBlock;               // ���� ��Ʈ�� ����.
    bool isGameOver;                // ���� ���� ����.

    public bool IsLockBlock => isLockBlock || isGameOver;

    private void Awake()
    {
        instance = this;
    }
    private void Start()
    {
        GenerateBlock();

        currentTime = MAX_TIME;
        isLockBlock = false;
        isLockTime = false;
        isGameOver = false;

        audioPlayer.SwitchBGM(true);
    }
    private void Update()
    {
        if (!isGameOver)
        {
            if (!isLockTime)
                currentTime = Mathf.Clamp(currentTime - Time.deltaTime, 0.0f, MAX_TIME);

            // UI ����.
            gameUI.UpdateTimer(currentTime, MAX_TIME);
            gameUI.UpdateScore(score);

            // ���� ���� üũ.
            isGameOver = currentTime <= 0.0f;
        }
    }


    [ContextMenu("���ο� ��� �����")]
    public void GenerateBlock()
    {
        // ��� �迭�� ������ ������ ���� �����Ѵ�.
        panelBlocks = new Block[BLOCK_WIDTH * BLOCK_HEIGHT];

        // ��ϵ��� �����ϱ� �� ���� ��ϵ��� ����
        for (int i = 0; i < panelBlocks.Length; i++)
        {
            if (panelBlocks[i] != null)
                DestroyImmediate(panelBlocks[i].gameObject);
        }

        // �迭�� ������� ���� ����� ã�� ������ �󿡼� ����
        Block[] anotherBlocks = FindObjectsOfType<Block>();
        foreach (Block block in anotherBlocks)
            DestroyImmediate(block.gameObject);

        // ��� ����.
        for (int i = 0; i < panelBlocks.Length; i++)
            panelBlocks[i] = CreateNewBlock(i);

        // �ߺ� ��� üũ.
        foreach (Block block in panelBlocks)
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
        List<Block> result = new List<Block>();
        if (block != null)
        {
            // �ּ�, �ִ� �ε����� ���ذ��� ��.
            int minIndex = isHorizontal ? block.info.y * BLOCK_WIDTH : block.info.x;
            int maxIndex = isHorizontal ? minIndex + BLOCK_WIDTH - 1 : minIndex + (BLOCK_WIDTH * (BLOCK_HEIGHT - 1));
            int addValue = isHorizontal ? 1 : BLOCK_WIDTH;

            for (int index = minIndex; index <= maxIndex; index += addValue)
            {
                List<Block> check = new List<Block>();
                for (int i = index; i <= maxIndex; i += addValue)
                {
                    if (panelBlocks[i] == null || block.id != panelBlocks[i].id)
                        break;

                    check.Add(panelBlocks[i]);
                }

                if (check.Contains(block) && check.Count > result.Count)
                    result = check;
            }
        }

        // ����� BlockGroup���� ��ȯ.
        BlockGroup group = new BlockGroup();
        group.blocks = result.ToArray();
        group.isHorizontal = isHorizontal;

        return group;
    }
    private Block[] MatchBlocks(params Block[] slideBlocks)
    {
        // �Ű������� �ش��ϴ� ��ϵ��� ���ҵ� ����̴�.
        // �ش� ����� �������� ��Ī�� �Ǵ�(=���ӵǴ� ������ 3�� �̻���)����� ã�� �����Ѵ�.
        List<Block> removeList = new List<Block>();
        foreach (Block block in slideBlocks)
        {
            BlockGroup horizontalGroup = SearchGroup(block, true);
            BlockGroup verticalGroup = SearchGroup(block, false);

            if (horizontalGroup.Length >= 3)
            {
                removeList.AddRange(horizontalGroup.blocks);
            }
            if (verticalGroup.Length >= 3)
            {
                removeList.AddRange(verticalGroup.blocks);
            }
        }

        // ������ ����� �ּ� 3�� �̻��� ��� ��ġ ����.
        return removeList.Distinct().ToArray();
    }
    private Vector3 IndexToLocalPos(int index)
    {
        Vector3 pivot = new Vector3((BLOCK_WIDTH / 2) * -1f, BLOCK_HEIGHT / 2, 0);
        int x = index % BLOCK_WIDTH;
        int y = index / BLOCK_WIDTH;
        return pivot + new Vector3(x, -y);
    }

    public Sprite GetSprite(ANIMAL id)
    {
        return animalSprites[(int)id];
    }

    // ��� ��ȯ �Լ�.
    public void SlideBlock(int index, VECTOR dir)
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
        if (targetIndex < 0 || targetIndex >= panelBlocks.Length || panelBlocks[targetIndex] == null)
            return;

        // ���� ������ �ƴ� ���.
        if (dir == VECTOR.LEFT || dir == VECTOR.RIGHT)
        {
            if (panelBlocks[index].info.y != panelBlocks[targetIndex].info.y)
                return;
        }

        StartCoroutine(IESlideBlock(index, targetIndex));
        return;
    }
    IEnumerator IESlideBlock(int index, int targetIndex)
    {
        isLockBlock = true;

        Block block = panelBlocks[index];
        Block targetBlock = panelBlocks[targetIndex];

        yield return StartCoroutine(IEExchangeBlock(block, targetBlock));        // ������ ���������� ����Ѵ�.

        // ��ġ�� �Ǿ��ٸ� ����� ������ �� �� ����� ä���.
        Block[] matchBlocks = MatchBlocks(block, targetBlock);
        
        if (matchBlocks.Length >= 3)
        { 
            int combo = 1;
            isLockTime = true;

            yield return StartCoroutine(IERemoveBlocks(matchBlocks, combo));
            yield return StartCoroutine(IEMediate());

            // ���ĺ��ʹ� ��� ����� ������� ��Ī���Ѻ���.
            while (true)
            {
                matchBlocks = MatchBlocks(panelBlocks);
                if(matchBlocks.Length < 3)
                    break;

                combo += 1;
                yield return StartCoroutine(IERemoveBlocks(matchBlocks, combo));
                yield return StartCoroutine(IEMediate());
            }
        }
        else
            yield return StartCoroutine(IEExchangeBlock(block, targetBlock));

        isLockTime = false;
        isLockBlock = false;
    }
    IEnumerator IEExchangeBlock(Block blockA, Block blockB)
    {
        const float SWAP_SPEED = 8f;

        // ������ ��ġ�� ��ȯ�Ѵ�.
        Vector3 posA = blockB.transform.localPosition;
        Vector3 posB = blockA.transform.localPosition;
        while (blockA.transform.localPosition != posA || blockB.transform.localPosition != posB)
        {
            blockA.transform.localPosition = Vector3.MoveTowards(blockA.transform.localPosition, posA, SWAP_SPEED * Time.deltaTime);
            blockB.transform.localPosition = Vector3.MoveTowards(blockB.transform.localPosition, posB, SWAP_SPEED * Time.deltaTime);
            yield return null;
        }

        SwapBlock(blockA.info.index, blockB.info.index);
    }
    private IEnumerator IERemoveBlocks(Block[] removeBlocks, int combo)
    {
        audioPlayer.PopBlock();
        score += CalculateScore(removeBlocks.Length, combo);
        foreach(Block block in removeBlocks)
            Instantiate(popFxPrefab, block.transform.position, Quaternion.identity);

        yield return new WaitForSeconds(0.15f);
        foreach (Block block in removeBlocks)
        {
            panelBlocks[block.info.index] = null;
            Destroy(block.gameObject);
        }
        yield return new WaitForSeconds(0.2f);
    }
    private int CalculateScore(int blockCount, int combo)
    {
        // 3��� 250 > ���� 1��� �߰����� 100�� �߰�
        // �޺��� ���ʽ� ���� +20%
        int addBlock = blockCount - 3;
        return Mathf.RoundToInt((250 + (100 * addBlock)) * (1 + ((combo - 1) * 0.2f)));
    }


    IEnumerator IEMediate()
    {
        MediateEmptySpace();        // MatchBlockToRemove()�� ���� �� ������ �߻����� ��� ����� ������.
        FillNewBlock();             // �� ������ ä���.

        const float FALL_SPEED = 8f;

        // ���ŵ� �ڽ��� ��ġ�� �̵��Ѵ�.
        int movingCount = 0;
        do
        {
            movingCount = 0;
            foreach (Block block in panelBlocks)
            {
                if (block == null)
                    continue;

                Vector3 destination = IndexToLocalPos(block.info.index);
                if (block.transform.localPosition != destination)
                {
                    movingCount++;
                    block.transform.localPosition = Vector3.MoveTowards(block.transform.localPosition, destination, FALL_SPEED * Time.deltaTime);
                }
            }
            yield return null;

        } while (movingCount > 0);
    }
    private void MediateEmptySpace()
    {
        // ������ ��Ϻ��� �������� �ڽ��� �Ʒ��� �� ����� �ִٸ� ������ �����Ѵ�.
        foreach (Block block in panelBlocks.Reverse())
        {
            if (block == null)
                continue;

            int nextIndex = block.info.index;
            while (true)
            {
                nextIndex += BLOCK_WIDTH;
                if (nextIndex >= panelBlocks.Length || panelBlocks[nextIndex] != null)
                    break;

                SwapBlock(block.info.index, nextIndex);
            }
        }
    }
    private void FillNewBlock()
    {
        // �� ��Ͽ� ���ο� ����� ������ �� ��ġ�� �����Ѵ�. (=�Ƹ� ȭ�� �� �������� �̵�)

        // �� x�� ���κ��� �� ����� ������ ����Ѵ�.
        int[] emptyCounts = new int[BLOCK_WIDTH];
        for(int index = 0; index < panelBlocks.Length; index++)
        {
            if (panelBlocks[index] == null)
                emptyCounts[index % BLOCK_WIDTH] += 1;
        }

        // ��� �� ����� �������� ���ο� ����� �����Ѵ�.
        for (int index = 0; index < panelBlocks.Length; index++)
        {
            if (panelBlocks[index] != null)
                continue;

            // ������ �� ����� ���� x�� ������ �� ������ ���� ��ġ�� �ø���.
            Block newBlock = CreateNewBlock(index);
            newBlock.transform.localPosition += Vector3.up * emptyCounts[index % BLOCK_WIDTH];
            panelBlocks[index] = newBlock;
        }
    }

    private Block CreateNewBlock(int index)
    {
        Block block = Instantiate(blockPrefab, transform);
        block.Setup(index, index % BLOCK_WIDTH, index / BLOCK_WIDTH);

        // ����� ��ġ�� ũ�⸦ �����Ѵ�.
        block.transform.localScale = new Vector3(1 / transform.localScale.x, 1 / transform.localScale.y, 1 / transform.localScale.z);
        block.transform.localPosition = IndexToLocalPos(index);
        return block;
    }
    private void SwapBlock(int indexA, int indexB)
    {
        Block blockA = panelBlocks[indexA];
        Block blockB = panelBlocks[indexB];

        // blocks �迭 ���� ��ȯ.
        panelBlocks[indexA] = blockB;
        panelBlocks[indexB] = blockA;

        // block���� info ��ȯ.
        if(blockA != null)
        {
            blockA.info.index = indexB;
            blockA.info.x = indexB % BLOCK_WIDTH;
            blockA.info.y = indexB / BLOCK_WIDTH;
            blockA.name = blockA.info.ToString();
        }
        if(blockB != null)
        {
            blockB.info.index = indexA;
            blockB.info.x = indexA % BLOCK_WIDTH;
            blockB.info.y = indexA / BLOCK_WIDTH;
            blockB.name = blockB.info.ToString();
        }
    }
}
