CREATE OR REPLACE FUNCTION mt_grams_query(text)
        RETURNS tsquery
        IMMUTABLE STRICT
        LANGUAGE "plpgsql"
AS $$
BEGIN
        RETURN (SELECT array_to_string(mt_grams_array($1), ' & ')::tsquery);
END
$$;
