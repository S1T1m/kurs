CREATE TABLE organizations (
    org_id INTEGER PRIMARY KEY AUTOINCREMENT,
    name TEXT NOT NULL,
    address TEXT,
    phone TEXT,
    inn TEXT UNIQUE CHECK(length(inn) = 10),
    bank_account TEXT,
    bik TEXT,
	okpo TEXT,
	
);

CREATE TABLE contract_types (
    type_id INTEGER PRIMARY KEY AUTOINCREMENT,
    name TEXT NOT NULL UNIQUE
);

CREATE TABLE stages (
    stage_id INTEGER PRIMARY KEY AUTOINCREMENT,
    name TEXT NOT NULL UNIQUE
);

CREATE TABLE vat_rates (
    vat_id INTEGER PRIMARY KEY AUTOINCREMENT,
    rate REAL NOT NULL CHECK(rate >= 0 AND rate <= 100)
);

CREATE TABLE contracts (
    contract_id INTEGER PRIMARY KEY AUTOINCREMENT,
    date_signed DATE NOT NULL DEFAULT CURRENT_DATE,
    customer_id INTEGER NOT NULL,
    contractor_id INTEGER NOT NULL,
    type_id INTEGER NOT NULL,
    stage_id INTEGER NOT NULL,
    vat_id INTEGER NOT NULL,
    due_date DATE,
    subject TEXT,
    note TEXT,
    FOREIGN KEY (customer_id) REFERENCES organizations(org_id) ON DELETE CASCADE,
    FOREIGN KEY (contractor_id) REFERENCES organizations(org_id) ON DELETE CASCADE,
    FOREIGN KEY (type_id) REFERENCES contract_types(type_id),
    FOREIGN KEY (stage_id) REFERENCES stages(stage_id),
    FOREIGN KEY (vat_id) REFERENCES vat_rates(vat_id)
);
CREATE INDEX idx_contracts_date ON contracts(date_signed);

CREATE TABLE contract_phases (
    contract_id INTEGER NOT NULL,
    phase_num INTEGER NOT NULL,
    due_date DATE,
    stage_id INTEGER,
    amount REAL CHECK(amount >= 0),
    advance REAL DEFAULT 0 CHECK(advance >= 0),
    subject TEXT,
    PRIMARY KEY (contract_id, phase_num),
    FOREIGN KEY (contract_id) REFERENCES contracts(contract_id) ON DELETE CASCADE,
    FOREIGN KEY (stage_id) REFERENCES stages(stage_id)
);

CREATE TABLE payment_types (
    payment_type_id INTEGER PRIMARY KEY AUTOINCREMENT,
    name TEXT NOT NULL UNIQUE
);

CREATE TABLE payments (
    payment_id INTEGER PRIMARY KEY AUTOINCREMENT,
    contract_id INTEGER NOT NULL,
    payment_date DATE NOT NULL DEFAULT CURRENT_DATE,
    amount REAL NOT NULL CHECK(amount >= 0),
    payment_type_id INTEGER NOT NULL,
    document_number TEXT,
    FOREIGN KEY (contract_id) REFERENCES contracts(contract_id) ON DELETE CASCADE,
    FOREIGN KEY (payment_type_id) REFERENCES payment_types(payment_type_id)
);
CREATE INDEX idx_payments_contract_date ON payments(contract_id, payment_date);

-- Просмотр по всем договорам с суммами оплат
CREATE VIEW v_contract_summary AS
SELECT 
    c.contract_id,
    c.date_signed,
    o1.name AS customer,
    o2.name AS contractor,
    t.name AS contract_type,
    SUM(p.amount) AS total_paid
FROM contracts c
JOIN organizations o1 ON c.customer_id = o1.org_id
JOIN organizations o2 ON c.contractor_id = o2.org_id
JOIN contract_types t ON c.type_id = t.type_id
LEFT JOIN payments p ON c.contract_id = p.contract_id
GROUP BY c.contract_id;

-- Просмотр дебиторской задолженности
CREATE VIEW v_debt AS
SELECT 
    c.contract_id,
    IFNULL(SUM(cp.amount), 0) AS planned,
    IFNULL(SUM(p.amount), 0) AS paid,
    (IFNULL(SUM(cp.amount), 0) - IFNULL(SUM(p.amount), 0)) AS debt
FROM contracts c
LEFT JOIN contract_phases cp ON c.contract_id = cp.contract_id
LEFT JOIN payments p ON c.contract_id = p.contract_id
GROUP BY c.contract_id
HAVING debt > 0;


-- Автоматическое обновление стадии при полной оплате
CREATE TRIGGER trg_update_stage_after_payment
AFTER INSERT ON payments
BEGIN
    UPDATE contracts
    SET stage_id = (SELECT stage_id FROM stages WHERE name = 'Оплачен')
    WHERE contract_id = NEW.contract_id
      AND (SELECT SUM(amount) FROM payments WHERE contract_id = NEW.contract_id) >=
          (SELECT SUM(amount) FROM contract_phases WHERE contract_id = NEW.contract_id);
END;


CREATE VIEW v_contract_info AS
SELECT 
    c.contract_id                        AS "Код договора",
    o1.name                              AS "Заказчик",
    o2.name                              AS "Исполнитель",
    t.name                               AS "Тип договора",
    s.name                               AS "Стадия",
    c.date_signed                        AS "Дата заключения",
    c.due_date                           AS "Дата исполнения",
    c.subject                            AS "Тема",
    IFNULL(SUM(cp.amount), 0)            AS "Плановая сумма",
    IFNULL(SUM(p.amount), 0)             AS "Оплачено",
    (IFNULL(SUM(cp.amount),0)-IFNULL(SUM(p.amount),0)) AS "Дебиторская задолженность"
FROM contracts c
LEFT JOIN organizations o1 ON c.customer_id = o1.org_id
LEFT JOIN organizations o2 ON c.contractor_id = o2.org_id
LEFT JOIN contract_types t  ON c.type_id = t.type_id
LEFT JOIN stages s          ON c.stage_id = s.stage_id
LEFT JOIN contract_phases cp ON cp.contract_id = c.contract_id
LEFT JOIN payments p        ON p.contract_id = c.contract_id
GROUP BY c.contract_id;


CREATE VIEW v_plan_schedule AS
SELECT 
    c.contract_id                 AS "Код договора",
    c.subject                     AS "Тема договора",
    cp.phase_num                  AS "Номер этапа",
    cp.due_date                   AS "Дата исполнения этапа",
    cp.amount                     AS "Сумма этапа",
    cp.advance                    AS "Сумма аванса",
    st.name                       AS "Стадия этапа"
FROM contract_phases cp
JOIN contracts c ON cp.contract_id = c.contract_id
LEFT JOIN stages st ON cp.stage_id = st.stage_id
ORDER BY c.contract_id, cp.phase_num;


CREATE VIEW v_payment_schedule AS
SELECT 
    c.contract_id                AS "Код договора",
    c.subject                    AS "Тема договора",
    p.payment_date               AS "Дата оплаты",
    p.amount                     AS "Сумма оплаты",
    pt.name                      AS "Вид оплаты",
    p.document_number            AS "№ платежного документа"
FROM payments p
JOIN contracts c ON p.contract_id = c.contract_id
JOIN payment_types pt ON p.payment_type_id = pt.payment_type_id
ORDER BY c.contract_id, p.payment_date;
