/*
DEV_LOWCODE_ARCHITECTURE_INVENTORY_001

Read-only SQL Server forensic inventory for the DEV low-code CS platform.

Target database: sgrt
Rules:
- SELECT only.
- Do not run UPDATE / DELETE / INSERT / ALTER / DROP / CREATE TABLE / TRUNCATE.
- Compatible with older SQL Server versions: no STRING_AGG, no JSON functions.
- TableName in JU_TemplateTable is the physical business table name.
*/

USE [sgrt];

/* R0 Environment Check */
SELECT
    'R0 Environment Check' AS ResultSetName,
    DB_NAME() AS DatabaseName,
    @@SERVERNAME AS ServerName,
    @@VERSION AS SqlServerVersion;

/* R1 Templates */
SELECT
    'R1 Templates' AS ResultSetName,
    a.CreateTime,
    a.TemplateName,
    a.TemplateTitle,
    a.LoadFilter,
    a.TemplateID
FROM JU_Template a
ORDER BY
    a.CreateTime DESC,
    a.TemplateID;

/* R1B Total Template Count */
SELECT
    'R1B Total Template Count' AS ResultSetName,
    COUNT(*) AS TOTAL_TEMPLATE_COUNT
FROM JU_Template;

/* R2 TemplateTable Mapping */
SELECT
    'R2 TemplateTable Mapping' AS ResultSetName,
    b.CreateTime,
    a.TemplateName,
    a.TemplateTitle,
    a.LoadFilter,
    b.TemplateID,
    b.TableDispName,
    b.TableName,
    b.TableID
FROM
    JU_Template a
RIGHT JOIN
    JU_TemplateTable b
ON
    a.TemplateID = b.TemplateID
ORDER BY
    b.CreateTime DESC,
    b.TemplateID,
    b.TableID;

/* R2B TemplateTable Count */
SELECT
    'R2B TemplateTable Count' AS ResultSetName,
    COUNT(*) AS TOTAL_TEMPLATE_TABLE_COUNT
FROM JU_TemplateTable;

/* R3 Physical Table Reuse */
SELECT
    'R3 Physical Table Reuse' AS ResultSetName,
    b.TableName,
    COUNT(*) AS TemplateTableRowCount,
    COUNT(DISTINCT b.TemplateID) AS TemplateCount,
    COUNT(DISTINCT b.TableID) AS TableIDCount
FROM
    JU_Template a
RIGHT JOIN
    JU_TemplateTable b
ON
    a.TemplateID = b.TemplateID
GROUP BY
    b.TableName
ORDER BY
    COUNT(DISTINCT b.TemplateID) DESC,
    COUNT(*) DESC,
    b.TableName;

/* R3B Physical Table Reuse Matrix */
SELECT
    'R3B Physical Table Reuse Matrix' AS ResultSetName,
    b.TableName,
    b.TemplateID,
    a.TemplateName,
    a.TemplateTitle,
    a.LoadFilter,
    b.TableID,
    b.TableDispName,
    b.CreateTime
FROM
    JU_Template a
RIGHT JOIN
    JU_TemplateTable b
ON
    a.TemplateID = b.TemplateID
ORDER BY
    b.TableName,
    b.TemplateID,
    b.TableID;

/* R3C Key Architecture Physical Tables */
SELECT
    'R3C Key Architecture Physical Tables' AS ResultSetName,
    b.TableName,
    COUNT(*) AS TemplateTableRowCount,
    COUNT(DISTINCT b.TemplateID) AS TemplateCount,
    COUNT(DISTINCT b.TableID) AS TableIDCount,
    STUFF((
        SELECT
            '; ' + CAST(b2.TemplateID AS varchar(20)) + ':' +
            ISNULL(a2.TemplateName, '') + '/' +
            ISNULL(a2.TemplateTitle, '') + '/TableID=' +
            CAST(b2.TableID AS varchar(20)) + '/LoadFilter=' +
            ISNULL(a2.LoadFilter, '')
        FROM JU_TemplateTable b2
        LEFT JOIN JU_Template a2
            ON a2.TemplateID = b2.TemplateID
        WHERE b2.TableName = b.TableName
        ORDER BY b2.TemplateID, b2.TableID
        FOR XML PATH(''), TYPE
    ).value('.', 'nvarchar(max)'), 1, 2, '') AS TemplateList
FROM JU_TemplateTable b
WHERE b.TableName IN (
    N'系统表',
    N'订单表',
    N'收发表',
    N'往来表',
    N'商品表',
    N'需求表',
    N'报价表',
    N'发票表',
    N'收付表',
    N'设备表',
    N'工序表',
    N'员工表'
)
GROUP BY b.TableName
ORDER BY
    COUNT(DISTINCT b.TemplateID) DESC,
    b.TableName;

/* R4 Order Templates */
SELECT
    'R4 Order Templates' AS ResultSetName,
    b.TemplateID,
    a.TemplateName,
    a.TemplateTitle,
    a.LoadFilter,
    b.TableID,
    b.TableDispName,
    b.TableName,
    b.CreateTime,
    CASE
        WHEN ISNULL(a.TemplateName, '') + ISNULL(a.TemplateTitle, '') + ISNULL(a.LoadFilter, '') LIKE N'%销售%' THEN N'销售候选'
        WHEN ISNULL(a.TemplateName, '') + ISNULL(a.TemplateTitle, '') + ISNULL(a.LoadFilter, '') LIKE N'%采购%' THEN N'采购候选'
        WHEN ISNULL(a.TemplateName, '') + ISNULL(a.TemplateTitle, '') + ISNULL(a.LoadFilter, '') LIKE N'%生产%' THEN N'生产候选'
        WHEN ISNULL(a.TemplateName, '') + ISNULL(a.TemplateTitle, '') + ISNULL(a.LoadFilter, '') LIKE N'%委外%' THEN N'委外候选'
        WHEN ISNULL(a.TemplateName, '') + ISNULL(a.TemplateTitle, '') + ISNULL(a.LoadFilter, '') LIKE N'%退%' THEN N'退货候选'
        ELSE N'未分类'
    END AS EvidenceBasedCategoryCandidate
FROM JU_TemplateTable b
LEFT JOIN JU_Template a
    ON a.TemplateID = b.TemplateID
WHERE b.TableName = N'订单表'
ORDER BY
    b.TemplateID,
    b.TableID;

/* R4B Order TableID Semantics */
SELECT
    'R4B Order TableID Semantics' AS ResultSetName,
    b.TableName,
    COUNT(*) AS TemplateTableRowCount,
    COUNT(DISTINCT b.TemplateID) AS TemplateCount,
    COUNT(DISTINCT b.TableID) AS TableIDCount,
    MIN(b.TableID) AS MinTableID,
    MAX(b.TableID) AS MaxTableID
FROM JU_TemplateTable b
WHERE b.TableName = N'订单表'
GROUP BY b.TableName;

/* R5 ReceiptIssue Templates */
SELECT
    'R5 ReceiptIssue Templates' AS ResultSetName,
    b.TemplateID,
    a.TemplateName,
    a.TemplateTitle,
    a.LoadFilter,
    b.TableID,
    b.TableDispName,
    b.TableName,
    b.CreateTime,
    CASE
        WHEN ISNULL(a.TemplateName, '') + ISNULL(a.TemplateTitle, '') + ISNULL(a.LoadFilter, '') LIKE N'%收货%' THEN N'收货候选'
        WHEN ISNULL(a.TemplateName, '') + ISNULL(a.TemplateTitle, '') + ISNULL(a.LoadFilter, '') LIKE N'%发货%' THEN N'发货候选'
        WHEN ISNULL(a.TemplateName, '') + ISNULL(a.TemplateTitle, '') + ISNULL(a.LoadFilter, '') LIKE N'%入库%' THEN N'入库候选'
        WHEN ISNULL(a.TemplateName, '') + ISNULL(a.TemplateTitle, '') + ISNULL(a.LoadFilter, '') LIKE N'%出库%' THEN N'出库候选'
        WHEN ISNULL(a.TemplateName, '') + ISNULL(a.TemplateTitle, '') + ISNULL(a.LoadFilter, '') LIKE N'%退%' THEN N'退货候选'
        WHEN ISNULL(a.TemplateName, '') + ISNULL(a.TemplateTitle, '') + ISNULL(a.LoadFilter, '') LIKE N'%调拨%' THEN N'调拨候选'
        WHEN ISNULL(a.TemplateName, '') + ISNULL(a.TemplateTitle, '') + ISNULL(a.LoadFilter, '') LIKE N'%生产%' THEN N'生产收发候选'
        ELSE N'未分类'
    END AS EvidenceBasedCategoryCandidate
FROM JU_TemplateTable b
LEFT JOIN JU_Template a
    ON a.TemplateID = b.TemplateID
WHERE b.TableName = N'收发表'
ORDER BY
    b.TemplateID,
    b.TableID;

/* R5B ReceiptIssue TableID Semantics */
SELECT
    'R5B ReceiptIssue TableID Semantics' AS ResultSetName,
    b.TableName,
    COUNT(*) AS TemplateTableRowCount,
    COUNT(DISTINCT b.TemplateID) AS TemplateCount,
    COUNT(DISTINCT b.TableID) AS TableIDCount,
    MIN(b.TableID) AS MinTableID,
    MAX(b.TableID) AS MaxTableID
FROM JU_TemplateTable b
WHERE b.TableName = N'收发表'
GROUP BY b.TableName;

/* R6 LoadFilters */
SELECT
    'R6 LoadFilters' AS ResultSetName,
    a.TemplateID,
    a.TemplateName,
    a.TemplateTitle,
    b.TableName,
    b.TableID,
    b.TableDispName,
    a.LoadFilter
FROM JU_Template a
INNER JOIN JU_TemplateTable b
    ON a.TemplateID = b.TemplateID
WHERE
    a.LoadFilter IS NOT NULL
    AND LTRIM(RTRIM(CAST(a.LoadFilter AS nvarchar(max)))) <> N''
ORDER BY
    b.TableName,
    a.TemplateID,
    b.TableID;

/* R6B LoadFilters For Core Tables */
SELECT
    'R6B LoadFilters For Core Tables' AS ResultSetName,
    a.TemplateID,
    a.TemplateName,
    a.TemplateTitle,
    b.TableName,
    b.TableID,
    b.TableDispName,
    a.LoadFilter
FROM JU_Template a
INNER JOIN JU_TemplateTable b
    ON a.TemplateID = b.TemplateID
WHERE
    b.TableName IN (N'订单表', N'收发表', N'往来表', N'商品表')
    AND a.LoadFilter IS NOT NULL
    AND LTRIM(RTRIM(CAST(a.LoadFilter AS nvarchar(max)))) <> N''
ORDER BY
    b.TableName,
    a.TemplateID,
    b.TableID;

/* R7 User Table Schemas */
SELECT
    'R7 User Table Schemas' AS ResultSetName,
    (CASE WHEN a.colorder = 1 THEN d.name ELSE '' END) AS 表名,
    d.name AS PhysicalTable,
    a.colorder AS 字段序号,
    a.name AS 字段名,
    (CASE
        WHEN COLUMNPROPERTY(a.id, a.name, 'IsIdentity') = 1
        THEN N'√'
        ELSE N''
     END) AS 标识,
    (CASE
        WHEN (
            SELECT COUNT(*)
            FROM sysobjects
            WHERE
                name IN (
                    SELECT name
                    FROM sysindexes
                    WHERE
                        id = a.id
                        AND indid IN (
                            SELECT indid
                            FROM sysindexkeys
                            WHERE
                                id = a.id
                                AND colid IN (
                                    SELECT colid
                                    FROM syscolumns
                                    WHERE
                                        id = a.id
                                        AND name = a.name
                                )
                        )
                )
                AND xtype = 'PK'
        ) > 0
        THEN N'√'
        ELSE N''
     END) AS 主键,
    b.name AS 类型,
    a.length AS 占用字节数,
    COLUMNPROPERTY(a.id, a.name, 'PRECISION') AS 长度,
    ISNULL(COLUMNPROPERTY(a.id, a.name, 'Scale'), 0) AS 小数位数,
    (CASE
        WHEN a.isnullable = 1
        THEN N'√'
        ELSE N''
     END) AS 允许空,
    ISNULL(e.text, '') AS 默认值,
    ISNULL(CAST(g.[value] AS nvarchar(4000)), '') AS 字段说明
FROM
    syscolumns a
LEFT JOIN
    systypes b
ON
    a.xtype = b.xusertype
INNER JOIN
    sysobjects d
ON
    a.id = d.id
    AND d.xtype = 'U'
    AND d.name <> 'dtproperties'
LEFT JOIN
    syscomments e
ON
    a.cdefault = e.id
LEFT JOIN
    sys.extended_properties g
ON
    a.id = g.major_id
    AND a.colid = g.minor_id
ORDER BY
    d.name,
    a.colorder;

/* R7B Core Table Schemas */
SELECT
    'R7B Core Table Schemas' AS ResultSetName,
    d.name AS PhysicalTable,
    a.colorder AS 字段序号,
    a.name AS 字段名,
    (CASE WHEN COLUMNPROPERTY(a.id, a.name, 'IsIdentity') = 1 THEN N'√' ELSE N'' END) AS 标识,
    (CASE
        WHEN (
            SELECT COUNT(*)
            FROM sysobjects
            WHERE
                name IN (
                    SELECT name
                    FROM sysindexes
                    WHERE
                        id = a.id
                        AND indid IN (
                            SELECT indid
                            FROM sysindexkeys
                            WHERE id = a.id AND colid = a.colid
                        )
                )
                AND xtype = 'PK'
        ) > 0
        THEN N'√'
        ELSE N''
     END) AS 主键,
    b.name AS 类型,
    a.length AS 占用字节数,
    COLUMNPROPERTY(a.id, a.name, 'PRECISION') AS PrecisionValue,
    ISNULL(COLUMNPROPERTY(a.id, a.name, 'Scale'), 0) AS ScaleValue,
    (CASE WHEN a.isnullable = 1 THEN N'√' ELSE N'' END) AS Nullable,
    ISNULL(e.text, '') AS DefaultValue,
    ISNULL(CAST(g.[value] AS nvarchar(4000)), '') AS Description
FROM syscolumns a
LEFT JOIN systypes b
    ON a.xtype = b.xusertype
INNER JOIN sysobjects d
    ON a.id = d.id
    AND d.xtype = 'U'
    AND d.name <> 'dtproperties'
LEFT JOIN syscomments e
    ON a.cdefault = e.id
LEFT JOIN sys.extended_properties g
    ON a.id = g.major_id
    AND a.colid = g.minor_id
WHERE d.name IN (
    N'订单表',
    N'收发表',
    N'往来表',
    N'商品表',
    N'发票表',
    N'收付表'
)
ORDER BY
    d.name,
    a.colorder;

/* R8 Numeric Precision */
SELECT
    'R8 Numeric Precision' AS ResultSetName,
    d.name AS PhysicalTable,
    a.colorder AS ColumnOrder,
    a.name AS ColumnName,
    b.name AS DataType,
    COLUMNPROPERTY(a.id, a.name, 'PRECISION') AS PrecisionValue,
    ISNULL(COLUMNPROPERTY(a.id, a.name, 'Scale'), 0) AS ScaleValue,
    ISNULL(CAST(g.[value] AS nvarchar(4000)), '') AS Description,
    CASE
        WHEN a.name LIKE N'%数量%' OR a.name LIKE N'%Qty%' OR a.name LIKE N'%Quantity%' THEN N'QuantityCandidate'
        WHEN a.name LIKE N'%单价%' OR a.name LIKE N'%Price%' THEN N'PriceCandidate'
        WHEN a.name LIKE N'%金额%' OR a.name LIKE N'%Amount%' THEN N'AmountCandidate'
        WHEN a.name LIKE N'%税%' OR a.name LIKE N'%Tax%' THEN N'TaxCandidate'
        WHEN a.name LIKE N'%折扣%' OR a.name LIKE N'%Discount%' THEN N'DiscountCandidate'
        WHEN a.name LIKE N'%汇率%' OR a.name LIKE N'%Rate%' THEN N'RateCandidate'
        WHEN a.name LIKE N'%成本%' OR a.name LIKE N'%Cost%' THEN N'CostCandidate'
        ELSE N'NumericOther'
    END AS NumericSemanticCandidate
FROM syscolumns a
LEFT JOIN systypes b
    ON a.xtype = b.xusertype
INNER JOIN sysobjects d
    ON a.id = d.id
    AND d.xtype = 'U'
    AND d.name <> 'dtproperties'
LEFT JOIN sys.extended_properties g
    ON a.id = g.major_id
    AND a.colid = g.minor_id
WHERE
    b.name IN (N'decimal', N'numeric', N'money', N'smallmoney', N'float', N'real', N'int', N'bigint', N'smallint', N'tinyint')
    AND (
        d.name IN (N'订单表', N'收发表', N'往来表', N'商品表', N'发票表', N'收付表')
        OR a.name LIKE N'%数量%'
        OR a.name LIKE N'%Qty%'
        OR a.name LIKE N'%Quantity%'
        OR a.name LIKE N'%单价%'
        OR a.name LIKE N'%Price%'
        OR a.name LIKE N'%金额%'
        OR a.name LIKE N'%Amount%'
        OR a.name LIKE N'%税%'
        OR a.name LIKE N'%Tax%'
        OR a.name LIKE N'%折扣%'
        OR a.name LIKE N'%Discount%'
        OR a.name LIKE N'%汇率%'
        OR a.name LIKE N'%Rate%'
        OR a.name LIKE N'%成本%'
        OR a.name LIKE N'%Cost%'
    )
ORDER BY
    d.name,
    a.colorder;

/* R9 TableID / TableName Semantics */
SELECT
    'R9 TableID TableName Semantics - By TableName' AS ResultSetName,
    b.TableName,
    COUNT(*) AS RowCount,
    COUNT(DISTINCT b.TemplateID) AS TemplateCount,
    COUNT(DISTINCT b.TableID) AS TableIDCount,
    MIN(b.TableID) AS MinTableID,
    MAX(b.TableID) AS MaxTableID
FROM JU_TemplateTable b
GROUP BY b.TableName
ORDER BY
    COUNT(DISTINCT b.TableID) DESC,
    COUNT(DISTINCT b.TemplateID) DESC,
    b.TableName;

SELECT
    'R9B TableID TableName Semantics - By TableID' AS ResultSetName,
    b.TableID,
    COUNT(*) AS RowCount,
    COUNT(DISTINCT b.TableName) AS TableNameCount,
    MIN(b.TableName) AS ExampleMinTableName,
    MAX(b.TableName) AS ExampleMaxTableName
FROM JU_TemplateTable b
GROUP BY b.TableID
ORDER BY
    COUNT(DISTINCT b.TableName) DESC,
    b.TableID;

/* R10 Candidate Source / Reference Fields For Core Tables */
SELECT
    'R10 Candidate Source Reference Fields' AS ResultSetName,
    d.name AS PhysicalTable,
    a.colorder AS ColumnOrder,
    a.name AS ColumnName,
    b.name AS DataType,
    COLUMNPROPERTY(a.id, a.name, 'PRECISION') AS PrecisionValue,
    ISNULL(COLUMNPROPERTY(a.id, a.name, 'Scale'), 0) AS ScaleValue,
    ISNULL(CAST(g.[value] AS nvarchar(4000)), '') AS Description
FROM syscolumns a
LEFT JOIN systypes b
    ON a.xtype = b.xusertype
INNER JOIN sysobjects d
    ON a.id = d.id
    AND d.xtype = 'U'
    AND d.name <> 'dtproperties'
LEFT JOIN sys.extended_properties g
    ON a.id = g.major_id
    AND a.colid = g.minor_id
WHERE
    d.name IN (N'订单表', N'收发表', N'往来表', N'商品表', N'发票表', N'收付表')
    AND (
        a.name LIKE N'%来源%'
        OR a.name LIKE N'%源%'
        OR a.name LIKE N'%引用%'
        OR a.name LIKE N'%关联%'
        OR a.name LIKE N'%订单%'
        OR a.name LIKE N'%明细%'
        OR a.name LIKE N'%审核%'
        OR a.name LIKE N'%复审%'
        OR a.name LIKE N'%状态%'
        OR a.name LIKE N'%作废%'
        OR a.name LIKE N'%关闭%'
        OR a.name LIKE N'%冲销%'
    )
ORDER BY
    d.name,
    a.colorder;
