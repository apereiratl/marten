CREATE OR REPLACE FUNCTION mt_grams_vector(text)
        RETURNS tsvector
        IMMUTABLE STRICT
        LANGUAGE "plpgsql"
AS $$
BEGIN
        RETURN (SELECT array_to_string(mt_grams_array($1), ' ')::tsvector);
END
$$;
