CREATE TABLE IF NOT EXISTS merchants (
    id varchar(9) PRIMARY KEY CHECK (id ~ '^[0-9]{9}$' AND id <> '000000000'),
    name text NOT NULL
);
CREATE TABLE IF NOT EXISTS devices (
    id varchar(15) PRIMARY KEY CHECK (id ~ '^[0-9]{15}$'),
    merchant_id varchar(9) NOT NULL REFERENCES merchants(id),
    active boolean NOT NULL DEFAULT true,
    encrypted_key bytea NOT NULL
);
CREATE TABLE IF NOT EXISTS connections (
    id uuid PRIMARY KEY,
    gateway text NOT NULL,
    opened_at timestamptz NOT NULL DEFAULT now(),
    last_activity_at timestamptz NOT NULL DEFAULT now(),
    closed_at timestamptz,
    close_reason text,
    device_id varchar(15) REFERENCES devices(id)
);
CREATE TABLE IF NOT EXISTS requests (
    device_id varchar(15) NOT NULL REFERENCES devices(id),
    request_id varchar(32) NOT NULL,
    canonical text NOT NULL,
    response text NOT NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    PRIMARY KEY (device_id, request_id)
);
CREATE TABLE IF NOT EXISTS operations (
    device_id varchar(15) NOT NULL,
    request_id varchar(32) NOT NULL,
    operand_a bigint NOT NULL,
    operand_b bigint NOT NULL,
    result bigint NOT NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    PRIMARY KEY (device_id, request_id),
    FOREIGN KEY (device_id, request_id) REFERENCES requests(device_id, request_id)
        DEFERRABLE INITIALLY DEFERRED
);
CREATE TABLE IF NOT EXISTS device_totals (
    device_id varchar(15) PRIMARY KEY REFERENCES devices(id),
    operation_count bigint NOT NULL DEFAULT 0 CHECK (operation_count >= 0),
    result_sum numeric(28,0) NOT NULL DEFAULT 0
);
CREATE TABLE IF NOT EXISTS message_attempts (
    event_id uuid PRIMARY KEY,
    connection_id uuid NOT NULL REFERENCES connections(id),
    received_at timestamptz NOT NULL DEFAULT now(),
    device_claim varchar(15),
    request_id varchar(32),
    action varchar(3),
    code varchar(3) NOT NULL,
    duplicate boolean NOT NULL DEFAULT false,
    canonical text NOT NULL,
    detail text
);
CREATE INDEX IF NOT EXISTS ix_attempts_connection ON message_attempts(connection_id);
CREATE INDEX IF NOT EXISTS ix_attempts_device_time ON message_attempts(device_claim, received_at);
CREATE INDEX IF NOT EXISTS ix_connections_activity ON connections(last_activity_at) WHERE closed_at IS NULL;
COMMENT ON TABLE requests IS 'Idempotency records are retained: do not truncate while replays are possible.';
