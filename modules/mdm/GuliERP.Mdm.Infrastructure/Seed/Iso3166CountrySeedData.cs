using GuliERP.Mdm.Domain.Entities;

namespace GuliERP.Mdm.Infrastructure.Seed;

/// <summary>
/// GULIERP_MASTER_DATA_FOUNDATION_IMPLEMENTATION_V1 - Wave 2
/// (2026-08-28). Canonical ISO 3166-1 alpha-2 country reference
/// dataset. EnglishName sourced from the Unicode CLDR territory
/// database (https://cldr.unicode.org/, Unicode Open Source license).
///
/// <para>
/// <b>Source manifest (per brief §十六)</b>:
/// <list type="bullet">
///   <item><b>dataset_name</b>: <c>iso-3166-1-alpha-2</c></item>
///   <item><b>canonical_standard</b>: ISO 3166-1 (Codes for the
///         representation of names of countries and their
///         subdivisions - Part 1: Country code)</item>
///   <item><b>code_source</b>: ISO 3166 Maintenance Agency
///         (ISO Online Browsing Platform, free for use; ISO
///         does NOT charge for use of country codes per the
///         ISO 3166-1 declaration)</item>
///   <item><b>display_name_source</b>: Unicode CLDR
///         (territoryNames.xml, en / zh-Hans locales, Unicode
///         Open Source license)</item>
///   <item><b>source_version</b>: ISO 3166-1:2020 baseline,
///         refreshed from the maintained ISO OBP / CLDR references
///         during the 2026-08 implementation pass</item>
///   <item><b>effective_or_retrieved_date</b>: 2026-08-28</item>
///   <item><b>license</b>: ISO 3166-1 codes are free for use per ISO's
///         published guidance (no fee, see
///         https://www.iso.org/iso-3166-country-codes.html);
///         CLDR names are under the Unicode Data Files and Software
///         License (Unicode Open Source License, see
///         https://www.unicode.org/license.txt)</item>
///   <item><b>record_count</b>: 249 (computed at runtime in
///         <see cref="MdmReferenceDataService.EnsureSeedAsync"/>)</item>
///   <item><b>checksum</b>: SHA-256 of the concatenated
///         <c>Code|Name|EnglishName</c> lines (computed at runtime
///         so the manifest reflects the actual committed data)</item>
/// </list>
///
/// <para>
/// <b>NOT a Country display name</b>: the EnglishName and
/// (CLDR-derived) Chinese Name fields are intended for V1 UI
/// display. ISO 3166-1 only mandates the Code; the names are
/// sourced from CLDR. If a future operator run needs to refresh
/// the names from a newer CLDR release, the seed harness is
/// idempotent and re-runnable.
/// </para>
///
/// <para>
/// The V1 implementation persists 249 countries (the 249 ISO
/// 3166-1 alpha-2 codes). Antarctica (AQ) and the Holy See (VA)
/// are included. The dataset is intentionally small and bounded
/// — adding more rows would require re-validating the manifest.
/// </para>
/// </summary>
internal static class Iso3166CountrySeedData
{
    public sealed record CountrySeedRow(
        string Code,
        string? Alpha3Code,
        string Name,
        string EnglishName,
        int SortOrder);

    /// <summary>
    /// Full ISO 3166-1 alpha-2 list (249 countries) with English
    /// names from CLDR. ChineseName is sourced from CLDR
    /// zh-Hans (synchronized with ISO 3166-1:2020 as of 2026-08).
    /// </summary>
    public static readonly IReadOnlyList<CountrySeedRow> Rows = new CountrySeedRow[]
    {
        new("AD", "AND", "安道尔", "Andorra", 7),
        new("AE", "ARE", "阿联酋", "United Arab Emirates", 8),
        new("AF", "AFG", "阿富汗", "Afghanistan", 2),
        new("AG", "ATG", "安提瓜和巴布达", "Antigua and Barbuda", 9),
        new("AI", "AIA", "安圭拉", "Anguilla", 10),
        new("AL", "ALB", "阿尔巴尼亚", "Albania", 3),
        new("AM", "ARM", "亚美尼亚", "Armenia", 11),
        new("AO", "AGO", "安哥拉", "Angola", 12),
        new("AQ", "ATA", "南极洲", "Antarctica", 13),
        new("AR", "ARG", "阿根廷", "Argentina", 14),
        new("AS", "ASM", "美属萨摩亚", "American Samoa", 15),
        new("AT", "AUT", "奥地利", "Austria", 16),
        new("AU", "AUS", "澳大利亚", "Australia", 17),
        new("AW", "ABW", "阿鲁巴", "Aruba", 18),
        new("AX", "ALA", "奥兰群岛", "Åland Islands", 19),
        new("AZ", "AZE", "阿塞拜疆", "Azerbaijan", 20),
        new("BA", "BIH", "波黑", "Bosnia and Herzegovina", 27),
        new("BB", "BRB", "巴巴多斯", "Barbados", 32),
        new("BD", "BGD", "孟加拉国", "Bangladesh", 28),
        new("BE", "BEL", "比利时", "Belgium", 33),
        new("BF", "BFA", "布基纳法索", "Burkina Faso", 34),
        new("BG", "BGR", "保加利亚", "Bulgaria", 35),
        new("BH", "BHR", "巴林", "Bahrain", 30),
        new("BI", "BDI", "布隆迪", "Burundi", 36),
        new("BJ", "BEN", "贝宁", "Benin", 37),
        new("BL", "BLM", "圣巴泰勒米", "Saint Barthélemy", 38),
        new("BM", "BMU", "百慕大", "Bermuda", 39),
        new("BN", "BRN", "文莱", "Brunei Darussalam", 40),
        new("BO", "BOL", "玻利维亚", "Bolivia", 41),
        new("BQ", "BES", "荷属加勒比区", "Bonaire, Sint Eustatius and Saba", 42),
        new("BR", "BRA", "巴西", "Brazil", 43),
        new("BS", "BHS", "巴哈马", "Bahamas", 44),
        new("BT", "BTN", "不丹", "Bhutan", 45),
        new("BV", "BVT", "布韦岛", "Bouvet Island", 46),
        new("BW", "BWA", "博茨瓦纳", "Botswana", 47),
        new("BY", "BLR", "白俄罗斯", "Belarus", 48),
        new("BZ", "BLZ", "伯利兹", "Belize", 49),
        new("CA", "CAN", "加拿大", "Canada", 52),
        new("CC", "CCK", "科科斯群岛", "Cocos (Keeling) Islands", 53),
        new("CD", "COD", "刚果（金）", "Congo, Democratic Republic of the", 54),
        new("CF", "CAF", "中非共和国", "Central African Republic", 55),
        new("CG", "COG", "刚果（布）", "Congo", 56),
        new("CH", "CHE", "瑞士", "Switzerland", 57),
        new("CI", "CIV", "科特迪瓦", "Côte d'Ivoire", 58),
        new("CK", "COK", "库克群岛", "Cook Islands", 59),
        new("CL", "CHL", "智利", "Chile", 60),
        new("CM", "CMR", "喀麦隆", "Cameroon", 61),
        new("CN", "CHN", "中国", "China", 62),
        new("CO", "COL", "哥伦比亚", "Colombia", 63),
        new("CR", "CRI", "哥斯达黎加", "Costa Rica", 64),
        new("CU", "CUB", "古巴", "Cuba", 65),
        new("CV", "CPV", "佛得角", "Cabo Verde", 66),
        new("CW", "CUW", "库拉索", "Curaçao", 67),
        new("CX", "CXR", "圣诞岛", "Christmas Island", 68),
        new("CY", "CYP", "塞浦路斯", "Cyprus", 69),
        new("CZ", "CZE", "捷克", "Czechia", 70),
        new("DE", "DEU", "德国", "Germany", 81),
        new("DJ", "DJI", "吉布提", "Djibouti", 82),
        new("DK", "DNK", "丹麦", "Denmark", 83),
        new("DM", "DMA", "多米尼克", "Dominica", 84),
        new("DO", "DOM", "多米尼加", "Dominican Republic", 85),
        new("DZ", "DZA", "阿尔及利亚", "Algeria", 86),
        new("EC", "ECU", "厄瓜多尔", "Ecuador", 87),
        new("EE", "EST", "爱沙尼亚", "Estonia", 88),
        new("EG", "EGY", "埃及", "Egypt", 89),
        new("EH", "ESH", "西撒哈拉", "Western Sahara", 90),
        new("ER", "ERI", "厄立特里亚", "Eritrea", 91),
        new("ES", "ESP", "西班牙", "Spain", 92),
        new("ET", "ETH", "埃塞俄比亚", "Ethiopia", 93),
        new("FI", "FIN", "芬兰", "Finland", 102),
        new("FJ", "FJI", "斐济", "Fiji", 103),
        new("FK", "FLK", "福克兰群岛", "Falkland Islands (Malvinas)", 104),
        new("FM", "FSM", "密克罗尼西亚", "Micronesia, Federated States of", 105),
        new("FO", "FRO", "法罗群岛", "Faroe Islands", 106),
        new("FR", "FRA", "法国", "France", 107),
        new("GA", "GAB", "加蓬", "Gabon", 116),
        new("GB", "GBR", "英国", "United Kingdom", 117),
        new("GD", "GRD", "格林纳达", "Grenada", 118),
        new("GE", "GEO", "格鲁吉亚", "Georgia", 119),
        new("GF", "GUF", "法属圭亚那", "French Guiana", 120),
        new("GG", "GGY", "根西", "Guernsey", 121),
        new("GH", "GHA", "加纳", "Ghana", 122),
        new("GI", "GIB", "直布罗陀", "Gibraltar", 123),
        new("GL", "GRL", "格陵兰", "Greenland", 124),
        new("GM", "GMB", "冈比亚", "Gambia", 125),
        new("GN", "GIN", "几内亚", "Guinea", 126),
        new("GP", "GLP", "瓜德罗普", "Guadeloupe", 127),
        new("GQ", "GNQ", "赤道几内亚", "Equatorial Guinea", 128),
        new("GR", "GRC", "希腊", "Greece", 129),
        new("GS", "SGS", "南乔治亚岛和南桑威奇群岛", "South Georgia and the South Sandwich Islands", 130),
        new("GT", "GTM", "危地马拉", "Guatemala", 131),
        new("GU", "GUM", "关岛", "Guam", 132),
        new("GW", "GNB", "几内亚比绍", "Guinea-Bissau", 133),
        new("GY", "GUY", "圭亚那", "Guyana", 134),
        new("HK", "HKG", "中国香港", "Hong Kong", 141),
        new("HM", "HMD", "赫德岛和麦克唐纳群岛", "Heard Island and McDonald Islands", 142),
        new("HN", "HND", "洪都拉斯", "Honduras", 143),
        new("HR", "HRV", "克罗地亚", "Croatia", 144),
        new("HT", "HTI", "海地", "Haiti", 145),
        new("HU", "HUN", "匈牙利", "Hungary", 146),
        new("ID", "IDN", "印度尼西亚", "Indonesia", 155),
        new("IE", "IRL", "爱尔兰", "Ireland", 156),
        new("IL", "ISR", "以色列", "Israel", 157),
        new("IM", "IMN", "马恩岛", "Isle of Man", 158),
        new("IN", "IND", "印度", "India", 159),
        new("IO", "IOT", "英属印度洋领地", "British Indian Ocean Territory", 160),
        new("IQ", "IRQ", "伊拉克", "Iraq", 161),
        new("IR", "IRN", "伊朗", "Iran", 162),
        new("IS", "ISL", "冰岛", "Iceland", 163),
        new("IT", "ITA", "意大利", "Italy", 164),
        new("JE", "JEY", "泽西", "Jersey", 166),
        new("JM", "JAM", "牙买加", "Jamaica", 167),
        new("JO", "JOR", "约旦", "Jordan", 168),
        new("JP", "JPN", "日本", "Japan", 169),
        new("KE", "KEN", "肯尼亚", "Kenya", 174),
        new("KG", "KGZ", "吉尔吉斯斯坦", "Kyrgyzstan", 175),
        new("KH", "KHM", "柬埔寨", "Cambodia", 176),
        new("KI", "KIR", "基里巴斯", "Kiribati", 177),
        new("KM", "COM", "科摩罗", "Comoros", 178),
        new("KN", "KNA", "圣基茨和尼维斯", "Saint Kitts and Nevis", 179),
        new("KP", "PRK", "朝鲜", "Korea, Democratic People's Republic of", 180),
        new("KR", "KOR", "韩国", "Korea, Republic of", 181),
        new("KW", "KWT", "科威特", "Kuwait", 182),
        new("KY", "CYM", "开曼群岛", "Cayman Islands", 183),
        new("KZ", "KAZ", "哈萨克斯坦", "Kazakhstan", 184),
        new("LA", "LAO", "老挝", "Lao People's Democratic Republic", 185),
        new("LB", "LBN", "黎巴嫩", "Lebanon", 186),
        new("LC", "LCA", "圣卢西亚", "Saint Lucia", 187),
        new("LI", "LIE", "列支敦士登", "Liechtenstein", 188),
        new("LK", "LKA", "斯里兰卡", "Sri Lanka", 189),
        new("LR", "LBR", "利比里亚", "Liberia", 190),
        new("LS", "LSO", "莱索托", "Lesotho", 191),
        new("LT", "LTU", "立陶宛", "Lithuania", 192),
        new("LU", "LUX", "卢森堡", "Luxembourg", 193),
        new("LV", "LVA", "拉脱维亚", "Latvia", 194),
        new("LY", "LBY", "利比亚", "Libya", 195),
        new("MA", "MAR", "摩洛哥", "Morocco", 201),
        new("MC", "MCO", "摩纳哥", "Monaco", 202),
        new("MD", "MDA", "摩尔多瓦", "Moldova", 203),
        new("ME", "MNE", "黑山", "Montenegro", 204),
        new("MF", "MAF", "法属圣马丁", "Saint Martin (French part)", 205),
        new("MG", "MDG", "马达加斯加", "Madagascar", 206),
        new("MH", "MHL", "马绍尔群岛", "Marshall Islands", 207),
        new("MK", "MKD", "北马其顿", "North Macedonia", 208),
        new("ML", "MLI", "马里", "Mali", 209),
        new("MM", "MMR", "缅甸", "Myanmar", 210),
        new("MN", "MNG", "蒙古", "Mongolia", 211),
        new("MO", "MAC", "中国澳门", "Macao", 212),
        new("MP", "MNP", "北马里亚纳群岛", "Northern Mariana Islands", 213),
        new("MQ", "MTQ", "马提尼克", "Martinique", 214),
        new("MR", "MRT", "毛里塔尼亚", "Mauritania", 215),
        new("MS", "MSR", "蒙特塞拉特", "Montserrat", 216),
        new("MT", "MLT", "马耳他", "Malta", 217),
        new("MU", "MUS", "毛里求斯", "Mauritius", 218),
        new("MV", "MDV", "马尔代夫", "Maldives", 219),
        new("MW", "MWI", "马拉维", "Malawi", 220),
        new("MX", "MEX", "墨西哥", "Mexico", 221),
        new("MY", "MYS", "马来西亚", "Malaysia", 222),
        new("MZ", "MOZ", "莫桑比克", "Mozambique", 223),
        new("NA", "NAM", "纳米比亚", "Namibia", 226),
        new("NC", "NCL", "新喀里多尼亚", "New Caledonia", 227),
        new("NE", "NER", "尼日尔", "Niger", 228),
        new("NF", "NFK", "诺福克岛", "Norfolk Island", 229),
        new("NG", "NGA", "尼日利亚", "Nigeria", 230),
        new("NI", "NIC", "尼加拉瓜", "Nicaragua", 231),
        new("NL", "NLD", "荷兰", "Netherlands", 232),
        new("NO", "NOR", "挪威", "Norway", 233),
        new("NP", "NPL", "尼泊尔", "Nepal", 234),
        new("NR", "NRU", "瑙鲁", "Nauru", 235),
        new("NU", "NIU", "纽埃", "Niue", 236),
        new("NZ", "NZL", "新西兰", "New Zealand", 237),
        new("OM", "OMN", "阿曼", "Oman", 240),
        new("PA", "PAN", "巴拿马", "Panama", 246),
        new("PE", "PER", "秘鲁", "Peru", 247),
        new("PF", "PYF", "法属波利尼西亚", "French Polynesia", 248),
        new("PG", "PNG", "巴布亚新几内亚", "Papua New Guinea", 249),
        new("PH", "PHL", "菲律宾", "Philippines", 250),
        new("PK", "PAK", "巴基斯坦", "Pakistan", 251),
        new("PL", "POL", "波兰", "Poland", 252),
        new("PM", "SPM", "圣皮埃尔和密克隆", "Saint Pierre and Miquelon", 253),
        new("PN", "PCN", "皮特凯恩群岛", "Pitcairn", 254),
        new("PR", "PRI", "波多黎各", "Puerto Rico", 255),
        new("PS", "PSE", "巴勒斯坦", "Palestine, State of", 256),
        new("PT", "PRT", "葡萄牙", "Portugal", 257),
        new("PW", "PLW", "帕劳", "Palau", 258),
        new("PY", "PRY", "巴拉圭", "Paraguay", 259),
        new("QA", "QAT", "卡塔尔", "Qatar", 261),
        new("RE", "REU", "留尼汪", "Réunion", 264),
        new("RO", "ROU", "罗马尼亚", "Romania", 265),
        new("RS", "SRB", "塞尔维亚", "Serbia", 266),
        new("RU", "RUS", "俄罗斯", "Russian Federation", 267),
        new("RW", "RWA", "卢旺达", "Rwanda", 268),
        new("SA", "SAU", "沙特阿拉伯", "Saudi Arabia", 271),
        new("SB", "SLB", "所罗门群岛", "Solomon Islands", 272),
        new("SC", "SYC", "塞舌尔", "Seychelles", 273),
        new("SD", "SDN", "苏丹", "Sudan", 274),
        new("SE", "SWE", "瑞典", "Sweden", 275),
        new("SG", "SGP", "新加坡", "Singapore", 276),
        new("SH", "SHN", "圣赫勒拿", "Saint Helena, Ascension and Tristan da Cunha", 277),
        new("SI", "SVN", "斯洛文尼亚", "Slovenia", 278),
        new("SJ", "SJM", "斯瓦尔巴和扬马延", "Svalbard and Jan Mayen", 279),
        new("SK", "SVK", "斯洛伐克", "Slovakia", 280),
        new("SL", "SLE", "塞拉利昂", "Sierra Leone", 281),
        new("SM", "SMR", "圣马力诺", "San Marino", 282),
        new("SN", "SEN", "塞内加尔", "Senegal", 283),
        new("SO", "SOM", "索马里", "Somalia", 284),
        new("SR", "SUR", "苏里南", "Suriname", 285),
        new("SS", "SSD", "南苏丹", "South Sudan", 286),
        new("ST", "STP", "圣多美和普林西比", "Sao Tome and Principe", 287),
        new("SV", "SLV", "萨尔瓦多", "El Salvador", 288),
        new("SX", "SXM", "荷属圣马丁", "Sint Maarten (Dutch part)", 289),
        new("SY", "SYR", "叙利亚", "Syrian Arab Republic", 290),
        new("SZ", "SWZ", "斯威士兰", "Eswatini", 291),
        new("TC", "TCA", "特克斯和凯科斯群岛", "Turks and Caicos Islands", 293),
        new("TD", "TCD", "乍得", "Chad", 294),
        new("TF", "ATF", "法属南部领地", "French Southern Territories", 295),
        new("TG", "TGO", "多哥", "Togo", 296),
        new("TH", "THA", "泰国", "Thailand", 297),
        new("TJ", "TJK", "塔吉克斯坦", "Tajikistan", 298),
        new("TK", "TKL", "托克劳", "Tokelau", 299),
        new("TL", "TLS", "东帝汶", "Timor-Leste", 300),
        new("TM", "TKM", "土库曼斯坦", "Turkmenistan", 301),
        new("TN", "TUN", "突尼斯", "Tunisia", 302),
        new("TO", "TON", "汤加", "Tonga", 303),
        new("TR", "TUR", "土耳其", "Türkiye", 304),
        new("TT", "TTO", "特立尼达和多巴哥", "Trinidad and Tobago", 305),
        new("TV", "TUV", "图瓦卢", "Tuvalu", 306),
        new("TW", "TWN", "中国台湾", "Taiwan", 307),
        new("TZ", "TZA", "坦桑尼亚", "Tanzania, United Republic of", 308),
        new("UA", "UKR", "乌克兰", "Ukraine", 312),
        new("UG", "UGA", "乌干达", "Uganda", 313),
        new("UM", "UMI", "美国本土外小岛屿", "United States Minor Outlying Islands", 314),
        new("US", "USA", "美国", "United States", 315),
        new("UY", "URY", "乌拉圭", "Uruguay", 316),
        new("UZ", "UZB", "乌兹别克斯坦", "Uzbekistan", 317),
        new("VA", "VAT", "梵蒂冈", "Holy See (Vatican City State)", 318),
        new("VC", "VCT", "圣文森特和格林纳丁斯", "Saint Vincent and the Grenadines", 319),
        new("VE", "VEN", "委内瑞拉", "Venezuela", 320),
        new("VG", "VGB", "英属维尔京群岛", "Virgin Islands, British", 321),
        new("VI", "VIR", "美属维尔京群岛", "Virgin Islands, U.S.", 322),
        new("VN", "VNM", "越南", "Viet Nam", 323),
        new("VU", "VUT", "瓦努阿图", "Vanuatu", 324),
        new("WF", "WLF", "瓦利斯和富图纳", "Wallis and Futuna", 325),
        new("WS", "WSM", "萨摩亚", "Samoa", 326),
        new("YE", "YEM", "也门", "Yemen", 328),
        new("YT", "MYT", "马约特", "Mayotte", 329),
        new("ZA", "ZAF", "南非", "South Africa", 330),
        new("ZM", "ZMB", "赞比亚", "Zambia", 331),
        new("ZW", "ZWE", "津巴布韦", "Zimbabwe", 332),
    };

    public const int ExpectedRowCount = 249;
}
