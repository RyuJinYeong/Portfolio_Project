using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class QuestNodeMapPanel : MonoBehaviour
{
    public TMP_Text questTitleText;
    public TMP_Text progressText;
    public TMP_Text statusText;
    public RectTransform lineContainer;
    public RectTransform nodeContainer;
    public QuestRouteNodeItemUI nodeItemPrefab;
    public Image linePrefab;

    [Header("Route Lines")]
    [Min(1f)] public float lineDashLength = 12f;
    [Min(0f)] public float lineDashGap = 8f;
    [Min(1f)] public float lineThickness = 3f;

    private readonly List<GameObject> spawnedNodes = new();
    private readonly List<GameObject> spawnedLines = new();
    private readonly Dictionary<int, RectTransform> nodeRects = new();
    private GameObject routeBoard;
    private QuestEncounterDialog encounterDialog;
    private Image panelBackground;
    private Color routeBackgroundColor;

    private void Awake()
    {
        Transform board = transform.Find("RouteBoard");
        routeBoard = board != null ? board.gameObject : null;
        encounterDialog = GetComponentInChildren<QuestEncounterDialog>(true);
        panelBackground = GetComponent<Image>();

        if (panelBackground != null)
            routeBackgroundColor = panelBackground.color;
    }

    public void Open()
    {
        QuestManager.Instance?.EnsureActiveRoute();

        ActiveQuestRuntime active = QuestManager.Instance != null
            ? QuestManager.Instance.active
            : null;

        if (active == null || active.def == null)
            return;

        gameObject.SetActive(true);
        UIManager.Instance?.SetExpeditionMapButtonVisible(false);

        if (panelBackground != null)
            panelBackground.color = routeBackgroundColor;

        if (routeBoard != null)
            routeBoard.SetActive(true);

        if (encounterDialog != null)
            encounterDialog.gameObject.SetActive(false);

        if (questTitleText != null)
            questTitleText.text = active.def.title;

        if (statusText != null)
            statusText.text = "연결된 다음 목적지 중 하나를 선택하세요.";

        Build(active);
        transform.SetAsLastSibling();
        UIManager.Instance?.SetExpeditionButtonState(true, false);
        UIManager.Instance?.BringExpeditionButtonsToFront();
    }

    public void Close()
    {
        Clear();
        gameObject.SetActive(false);
        bool expeditionActive = QuestManager.Instance != null &&
                                QuestManager.Instance.active != null &&
                                QuestManager.Instance.active.status == QuestStatus.Active;
        UIManager.Instance?.SetExpeditionButtonState(false, expeditionActive);
    }

    public void OpenEncounter()
    {
        Clear();
        gameObject.SetActive(true);
        UIManager.Instance?.SetExpeditionMapButtonVisible(false);

        if (routeBoard != null)
            routeBoard.SetActive(false);

        if (panelBackground != null)
            panelBackground.color = Color.clear;

        encounterDialog?.OpenCurrentEncounter();
        transform.SetAsLastSibling();
        UIManager.Instance?.SetExpeditionButtonState(true, false);
        UIManager.Instance?.BringExpeditionButtonsToFront();
    }

    public Transform GetRouteNodeTransform(int nodeId)
    {
        return nodeRects.TryGetValue(nodeId, out RectTransform nodeRect)
            ? nodeRect
            : null;
    }

    public QuestEncounterDialog GetEncounterDialog() => encounterDialog;

    public QuestEncounterDialog OpenSharedEncounter()
    {
        Clear();
        if (UIManager.Instance?.battleUiRoot != null)
            UIManager.Instance.battleUiRoot.SetActive(false);
        gameObject.SetActive(true);
        UIManager.Instance?.SetExpeditionMapButtonVisible(false);
        if (routeBoard != null)
            routeBoard.SetActive(false);
        if (panelBackground != null)
            panelBackground.color = Color.clear;
        if (encounterDialog != null)
            encounterDialog.gameObject.SetActive(true);
        transform.SetAsLastSibling();
        UIManager.Instance?.SetExpeditionButtonState(true, false);
        UIManager.Instance?.BringExpeditionButtonsToFront();
        return encounterDialog;
    }

    public void SetSharedStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;
    }

    private void Build(ActiveQuestRuntime active)
    {
        Clear();

        if (nodeContainer == null || nodeItemPrefab == null ||
            active.routeNodes == null || active.routeNodes.Count == 0)
        {
            return;
        }

        int maxDepth = 0;

        HashSet<int> detectedNodeIds = GetDetectedNodeIds(active);

        foreach (QuestRouteNode node in active.routeNodes)
        {
            if (node != null)
                maxDepth = Mathf.Max(maxDepth, node.depth);
        }

        foreach (QuestRouteNode node in active.routeNodes)
        {
            if (node == null)
                continue;

            QuestRouteNodeItemUI item = Instantiate(nodeItemPrefab, nodeContainer);
            RectTransform rect = item.transform as RectTransform;

            if (rect == null)
                continue;

            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = GetNodePosition(active.routeNodes, node, maxDepth);

            bool selectable = QuestManager.Instance.CanSelectRouteNode(node.id);
            bool current = active.currentRouteNodeId == node.id;
            bool detected = detectedNodeIds.Contains(node.id);
            item.Bind(node, detected, selectable && detected, current, SelectNode);

            spawnedNodes.Add(item.gameObject);
            nodeRects[node.id] = rect;
        }

        Canvas.ForceUpdateCanvases();
        BuildLines(active.routeNodes);

        if (progressText != null)
            progressText.text = $"진행 단계  {active.stageNodeIndex} / {maxDepth}";
    }

    private Vector2 GetNodePosition(
        List<QuestRouteNode> nodes,
        QuestRouteNode node,
        int maxDepth)
    {
        int layerCount = 0;

        foreach (QuestRouteNode candidate in nodes)
        {
            if (candidate != null && candidate.depth == node.depth)
                layerCount++;
        }

        float width = Mathf.Max(1f, nodeContainer.rect.width);
        float height = Mathf.Max(1f, nodeContainer.rect.height);
        float x = width * (node.depth + 0.5f) / (maxDepth + 1f);
        float ySpacing = height / (layerCount + 1f);
        float y = height * 0.5f - ySpacing * (node.column + 1);
        return new Vector2(x, y);
    }

    private void BuildLines(List<QuestRouteNode> nodes)
    {
        if (lineContainer == null || linePrefab == null)
            return;

        foreach (QuestRouteNode node in nodes)
        {
            if (node == null || node.nextNodeIds == null || !nodeRects.TryGetValue(node.id, out RectTransform from))
                continue;

            foreach (int nextId in node.nextNodeIds)
            {
                if (!nodeRects.TryGetValue(nextId, out RectTransform to))
                    continue;

                Vector2 start = from.anchoredPosition;
                Vector2 end = to.anchoredPosition;
                Vector2 delta = end - start;
                float distance = delta.magnitude;
                Vector2 direction = distance > 0f ? delta / distance : Vector2.right;
                float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
                float step = Mathf.Max(1f, lineDashLength + lineDashGap);

                for (float offset = 0f; offset < distance; offset += step)
                {
                    float dashLength = Mathf.Min(lineDashLength, distance - offset);
                    Image line = Instantiate(linePrefab, lineContainer);
                    line.gameObject.SetActive(true);
                    RectTransform rect = line.rectTransform;

                    rect.anchorMin = new Vector2(0f, 0.5f);
                    rect.anchorMax = new Vector2(0f, 0.5f);
                    rect.pivot = new Vector2(0f, 0.5f);
                    rect.anchoredPosition = start + direction * offset;
                    rect.sizeDelta = new Vector2(dashLength, lineThickness);
                    rect.localRotation = Quaternion.Euler(0f, 0f, angle);
                    line.color = node.cleared
                        ? new Color(0.68f, 0.52f, 0.28f, 0.9f)
                        : new Color(0.35f, 0.3f, 0.25f, 0.55f);
                    line.raycastTarget = false;

                    spawnedLines.Add(line.gameObject);
                }
            }
        }

        lineContainer.SetAsFirstSibling();
    }

    private HashSet<int> GetDetectedNodeIds(ActiveQuestRuntime active)
    {
        HashSet<int> detected = new HashSet<int>();

        if (active == null || active.routeNodes == null)
            return detected;

        Dictionary<int, QuestRouteNode> nodesById = new Dictionary<int, QuestRouteNode>();

        foreach (QuestRouteNode node in active.routeNodes)
        {
            if (node == null)
                continue;

            nodesById[node.id] = node;

            if (node.cleared || node.id == active.currentRouteNodeId)
                detected.Add(node.id);
        }

        if (!nodesById.TryGetValue(active.currentRouteNodeId, out QuestRouteNode current))
            return detected;

        int detectionRange = GetPartyDetectionRange(active);
        Queue<QuestRouteNode> queue = new Queue<QuestRouteNode>();
        Dictionary<int, int> distances = new Dictionary<int, int>
        {
            [current.id] = 0
        };
        queue.Enqueue(current);

        while (queue.Count > 0)
        {
            QuestRouteNode node = queue.Dequeue();
            int distance = distances[node.id];

            if (distance >= detectionRange || node.nextNodeIds == null)
                continue;

            foreach (int nextId in node.nextNodeIds)
            {
                if (!nodesById.TryGetValue(nextId, out QuestRouteNode next) ||
                    distances.ContainsKey(nextId))
                {
                    continue;
                }

                int nextDistance = distance + 1;
                distances[nextId] = nextDistance;
                detected.Add(nextId);
                queue.Enqueue(next);
            }
        }

        return detected;
    }

    private static int GetPartyDetectionRange(ActiveQuestRuntime active)
    {
        int highestMapDetectionRange = 0;

        if (active?.partyCharacterIds != null)
        {
            foreach (string characterId in active.partyCharacterIds)
            {
                CharacterManager manager = CharacterPoolManager.Instance != null
                    ? CharacterPoolManager.Instance.Get(characterId)
                    : null;

                if (manager == null && GameManager.Instance != null)
                {
                    foreach (CharacterManager candidate in GameManager.Instance.GetAllCharacters())
                    {
                        if (candidate?.character != null && candidate.character.ID == characterId)
                        {
                            manager = candidate;
                            break;
                        }
                    }
                }

                CharacterSpecialStats specialStats = manager?.character?.FinalSpecialStats;

                if (specialStats != null)
                {
                    highestMapDetectionRange = Mathf.Max(
                        highestMapDetectionRange,
                        specialStats.MapDetectionRange);
                }
            }
        }

        int tier = Mathf.Max(1, active?.def?.tier ?? 1);
        return Mathf.Max(1, highestMapDetectionRange / tier);
    }

    private void SelectNode(QuestRouteNode node)
    {
        if (MultiplayerSession.Instance != null && MultiplayerSession.Instance.InExpedition)
        {
            if (node != null) MultiplayerSession.Instance.ChooseExpeditionOption(node.id.ToString());
            return;
        }
        if (node == null || QuestManager.Instance == null || !QuestManager.Instance.SelectRouteNode(node.id))
        {
            if (statusText != null)
                statusText.text = "현재 위치에서 이동할 수 없는 목적지입니다.";

            return;
        }

        PlayerData playerData = PlayerManager.Instance != null
            ? PlayerManager.Instance.GetCurrentPlayerData()
            : null;

        if (playerData != null)
        {
            playerData.currentStage = QuestManager.Instance.active.stageKey;
            PlayerManager.Instance.SavePlayerDataToPlayFab();
        }

        if (node.type == QuestRouteNodeType.RandomEncounter ||
            node.type == QuestRouteNodeType.Rest)
        {
            Clear();

            if (routeBoard != null)
                routeBoard.SetActive(false);
        }
        else
        {
            Close();
        }

        GameManager.Instance?.EnterQuestRouteNode(node);
    }

    private void Clear()
    {
        foreach (GameObject node in spawnedNodes)
        {
            if (node != null)
                Destroy(node);
        }

        foreach (GameObject line in spawnedLines)
        {
            if (line != null)
                Destroy(line);
        }

        spawnedNodes.Clear();
        spawnedLines.Clear();
        nodeRects.Clear();
    }
}
